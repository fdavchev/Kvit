using Kvit.Application.Dispatching;
using Kvit.Application.Queries.PersonalEntries;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Me;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Auth;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.Me
{
    public sealed class GetMyDataExportQueryHandler(
        ICurrentUserProvider _currentUserProvider,
        AppDbContext _context,
        TimeProvider _timeProvider) : IQueryHandler<GetMyDataExportQuery, MyDataExportResponse>
    {
        private const string PrivacyNoticeUrl = "https://kvit-mk.pages.dev/privacy";

        public async Task<Result<MyDataExportResponse>> Handle(GetMyDataExportQuery query, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return Result.Unauthorized<MyDataExportResponse>(userId.Error, userId.ErrorCode);
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            ExportedProfile? profile = await ExportedProfileOf(userId.Value).SingleOrDefaultAsync(cancellationToken);
            if (profile is null)
            {
                return Result.Unauthorized<MyDataExportResponse>($"The signed-in account {userId.Value} no longer exists.", ResultCodes.AUTH_NOT_SIGNED_IN);
            }

            List<ExportedGroup> groups = await ExportedGroupsOfAsync(userId.Value, now, cancellationToken);
            ExportedPersonalBudget personalBudget = await ExportedPersonalBudgetOfAsync(userId.Value, cancellationToken);

            return Result.Ok(new MyDataExportResponse(now, PrivacyNoticeUrl, profile, personalBudget, groups));
        }

        private IQueryable<ExportedProfile> ExportedProfileOf(Guid userId)
        {
            return _context.Users
                .AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => new ExportedProfile(
                    user.Id,
                    user.DisplayName,
                    user.Email,
                    user.Language,
                    user.TimeZone,
                    user.CreatedAt,
                    user.IsGuest,
                    user.PasswordHash != null,
                    _context.UserLogins.Any(login => login.UserId == user.Id && login.LoginProvider == AccountService.GoogleLoginProvider),
                    user.BudgetTotalCurrency,
                    user.TermsVersion,
                    user.TermsAcceptedAt));
        }

        private async Task<List<ExportedGroup>> ExportedGroupsOfAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            List<MyGroupMembershipDto> memberships = await MyGroupMembershipDtosOf(userId).ToListAsync(cancellationToken);
            List<Guid> groupIds = [.. memberships.Select(membership => membership.Member.GroupId).Distinct()];
            IQueryable<GroupMember> myMemberRows = _context.GroupMembers.Where(member => member.UserId == userId);
            ILookup<Guid, ExportedMember> members = (await ExportedMembersOf(groupIds, userId, myMemberRows).ToListAsync(cancellationToken))
                .ToLookup(row => row.GroupId, row => row.Item);
            ILookup<Guid, ExportedExpense> expenses = (await ExportedExpensesOf(groupIds, myMemberRows).ToListAsync(cancellationToken))
                .ToLookup(row => row.GroupId, row => row.Item);
            ILookup<Guid, ExportedPayment> payments = (await ExportedPaymentsOf(groupIds, myMemberRows).ToListAsync(cancellationToken))
                .ToLookup(row => row.GroupId, row => row.Item);

            return [.. memberships
                .GroupBy(membership => membership.Member.GroupId)
                .Select(LatestMembershipOf)
                .Select(membership => new ExportedGroup(
                    membership.Member.GroupId,
                    membership.GroupName,
                    membership.GroupEmoji,
                    membership.GroupDefaultCurrency,
                    Group.EffectiveStatusOf(membership.GroupStatus, membership.GroupClosingStartedAt, now),
                    membership.IsOwner,
                    membership.Member.CountsInBudget,
                    new ExportedMembership(membership.Member.Id, membership.Member.JoinedAt, membership.Member.RemovedAt, membership.Member.EndKind),
                    [.. members[membership.Member.GroupId]],
                    [.. expenses[membership.Member.GroupId]],
                    [.. payments[membership.Member.GroupId]]))];
        }

        private static MyGroupMembershipDto LatestMembershipOf(IEnumerable<MyGroupMembershipDto> rowsInOneGroup)
        {
            return rowsInOneGroup
                .OrderByDescending(row => row.Member.RemovedAt ?? DateTimeOffset.MaxValue)
                .ThenByDescending(row => row.Member.JoinedAt)
                .First();
        }

        private IQueryable<MyGroupMembershipDto> MyGroupMembershipDtosOf(Guid userId)
        {
            return _context.GroupMembers
                .AsNoTracking()
                .Where(member => member.UserId == userId)
                .Join(
                    _context.Groups.Where(group => group.DeletedAt == null),
                    member => member.GroupId,
                    group => group.Id,
                    (member, group) => new { Member = member, Group = group })
                .OrderBy(row => row.Group.CreatedAt)
                .ThenBy(row => row.Group.Id)
                .Select(row => new MyGroupMembershipDto(
                    row.Member,
                    row.Group.Name,
                    row.Group.Emoji,
                    row.Group.DefaultCurrency,
                    row.Group.Status,
                    row.Group.ClosingStartedAt,
                    row.Group.OwnerUserId == userId));
        }

        private IQueryable<GroupItemDto<ExportedMember>> ExportedMembersOf(List<Guid> groupIds, Guid userId, IQueryable<GroupMember> myMemberRows)
        {
            return _context.NamedGroupMembers(groupIds)
                .AsNoTracking()
                .Where(named => named.Member.UserId == userId
                    || myMemberRows.Any(mine => mine.GroupId == named.Member.GroupId && (mine.RemovedAt == null || mine.RemovedAt >= named.Member.JoinedAt)))
                .InJoiningOrder()
                .Select(named => new
                {
                    Named = named,
                    EndIsVisible = myMemberRows.Any(mine => mine.GroupId == named.Member.GroupId && (mine.RemovedAt == null || mine.RemovedAt >= named.Member.RemovedAt)),
                })
                .Select(row => new GroupItemDto<ExportedMember>(
                    row.Named.Member.GroupId,
                    new ExportedMember(
                        row.Named.Member.Id,
                        row.Named.DisplayName,
                        row.Named.Member.UserId == null,
                        row.Named.Member.UserId == userId,
                        row.Named.Member.JoinedAt,
                        row.EndIsVisible ? row.Named.Member.RemovedAt : null,
                        row.EndIsVisible ? row.Named.Member.EndKind : null)));
        }

        private IQueryable<GroupItemDto<ExportedExpense>> ExportedExpensesOf(List<Guid> groupIds, IQueryable<GroupMember> myMemberRows)
        {
            return _context.Expenses
                .AsNoTracking()
                .Where(expense => groupIds.Contains(expense.GroupId) && expense.DeletedAt == null)
                .Where(expense => myMemberRows.Any(mine => mine.GroupId == expense.GroupId && (mine.RemovedAt == null || mine.RemovedAt >= expense.CreatedAt)))
                .OrderBy(expense => expense.ExpenseDate)
                .ThenBy(expense => expense.CreatedAt)
                .ThenBy(expense => expense.Id)
                .Select(expense => new GroupItemDto<ExportedExpense>(
                    expense.GroupId,
                    new ExportedExpense(
                        expense.Id,
                        expense.Title,
                        expense.Note,
                        expense.AmountMinor,
                        expense.Currency,
                        expense.ExpenseDate,
                        _context.Categories.Where(category => category.Id == expense.CategoryId).Select(category => category.Key).FirstOrDefault(),
                        expense.PaidByMemberId,
                        expense.SplitType,
                        expense.CreatedAt,
                        expense.Shares
                            .OrderBy(share => share.MemberId)
                            .Select(share => new ExportedShare(share.MemberId, share.ShareMinor))
                            .ToList())));
        }

        private IQueryable<GroupItemDto<ExportedPayment>> ExportedPaymentsOf(List<Guid> groupIds, IQueryable<GroupMember> myMemberRows)
        {
            return _context.Settlements
                .AsNoTracking()
                .Where(settlement => groupIds.Contains(settlement.GroupId) && settlement.DeletedAt == null)
                .Where(settlement => myMemberRows.Any(mine => mine.GroupId == settlement.GroupId && (mine.RemovedAt == null || mine.RemovedAt >= settlement.CreatedAt)))
                .OrderBy(settlement => settlement.CreatedAt)
                .ThenBy(settlement => settlement.Id)
                .Select(settlement => new GroupItemDto<ExportedPayment>(
                    settlement.GroupId,
                    new ExportedPayment(
                        settlement.Id,
                        settlement.FromMemberId,
                        settlement.ToMemberId,
                        settlement.AmountMinor,
                        settlement.Currency,
                        settlement.Status,
                        settlement.CreatedAt,
                        settlement.ResolvedAt)));
        }

        private async Task<ExportedPersonalBudget> ExportedPersonalBudgetOfAsync(Guid userId, CancellationToken cancellationToken)
        {
            return new ExportedPersonalBudget(
                await ExportedCategoriesOf(userId).ToListAsync(cancellationToken),
                await ExportedEntriesOf(userId).ToListAsync(cancellationToken),
                await ExportedRecurringIncomesOf(userId).ToListAsync(cancellationToken),
                await ExportedBudgetLimitsOf(userId).ToListAsync(cancellationToken),
                await ExportedDuplicateAnswersOf(userId).ToListAsync(cancellationToken));
        }

        private IQueryable<ExportedCategory> ExportedCategoriesOf(Guid userId)
        {
            return _context.Categories
                .AsNoTracking()
                .Where(category => category.OwnerUserId == userId)
                .OrderBy(category => category.SortOrder)
                .ThenBy(category => category.Id)
                .Select(category => new ExportedCategory(
                    category.Id,
                    category.Name!,
                    category.Emoji,
                    category.Color,
                    category.SortOrder,
                    category.ArchivedAt));
        }

        private IQueryable<ExportedEntry> ExportedEntriesOf(Guid userId)
        {
            return _context.EntriesOf(userId)
                .OrderBy(entry => entry.EntryDate)
                .ThenBy(entry => entry.CreatedAt)
                .ThenBy(entry => entry.Id)
                .Select(entry => new ExportedEntry(
                    entry.Id,
                    entry.Kind,
                    entry.AmountMinor,
                    entry.Currency,
                    entry.EntryDate,
                    entry.CategoryId,
                    _context.Categories.Where(category => category.Id == entry.CategoryId).Select(category => category.Key ?? category.Name).FirstOrDefault(),
                    entry.Note,
                    entry.MkdPerEur,
                    entry.RateDate,
                    entry.RecurringIncomeId,
                    entry.CreatedAt,
                    entry.UpdatedAt));
        }

        private IQueryable<ExportedRecurringIncome> ExportedRecurringIncomesOf(Guid userId)
        {
            return _context.RecurringIncomes
                .AsNoTracking()
                .Where(income => income.UserId == userId)
                .OrderBy(income => income.CreatedAt)
                .ThenBy(income => income.Id)
                .Select(income => new ExportedRecurringIncome(
                    income.Id,
                    income.Title,
                    income.AmountMinor,
                    income.Currency,
                    income.Frequency,
                    income.DayOfMonth,
                    income.DayOfWeek,
                    income.StartsOn,
                    income.EndsOn,
                    income.CreatedAt,
                    income.StoppedAt));
        }

        private IQueryable<ExportedBudgetLimit> ExportedBudgetLimitsOf(Guid userId)
        {
            return _context.BudgetLimits
                .AsNoTracking()
                .Where(limit => limit.UserId == userId)
                .OrderBy(limit => limit.EffectiveFrom)
                .ThenBy(limit => limit.CategoryId)
                .ThenBy(limit => limit.Id)
                .Select(limit => new ExportedBudgetLimit(
                    limit.CategoryId,
                    limit.EffectiveFrom,
                    limit.LimitMinor,
                    limit.CreatedAt,
                    limit.UpdatedAt));
        }

        private IQueryable<ExportedDuplicateAnswer> ExportedDuplicateAnswersOf(Guid userId)
        {
            return _context.DuplicateAnswers
                .AsNoTracking()
                .Where(answer => answer.UserId == userId)
                .OrderBy(answer => answer.CreatedAt)
                .ThenBy(answer => answer.Id)
                .Select(answer => new ExportedDuplicateAnswer(
                    answer.PersonalEntryId,
                    answer.ExpenseId,
                    answer.Answer,
                    answer.CreatedAt));
        }
    }
}
