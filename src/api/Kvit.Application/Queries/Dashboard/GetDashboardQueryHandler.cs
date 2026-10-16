using Kvit.Application.Dispatching;
using Kvit.Application.Queries.Activity;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Dashboard;
using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.Dashboard
{
    public sealed class GetDashboardQueryHandler(
        ICurrentUserProvider _currentUserProvider,
        AppDbContext _context,
        TimeProvider _timeProvider) : IQueryHandler<GetDashboardQuery, DashboardResponse>
    {
        public const int RecentActivityLimit = 5;

        public async Task<Result<DashboardResponse>> Handle(GetDashboardQuery query, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return Result.Unauthorized<DashboardResponse>(userId.Error, userId.ErrorCode);
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            List<DashboardGroupDto> memberGroups = [.. (await DashboardGroupDtosOf(userId.Value).ToListAsync(cancellationToken))
                .Select(group => group with { Status = Group.EffectiveStatusOf(group.Status, group.ClosingStartedAt, now) })];
            List<DashboardGroupDto> unfinishedGroups = [.. memberGroups.Where(group => group.Status != GroupStatus.Finished)];
            Dictionary<Guid, IReadOnlyList<Money>> balancesByGroup = await CallerBalancesOfAsync(unfinishedGroups, cancellationToken);
            List<Money> allBalances = [.. balancesByGroup.Values.SelectMany(balances => balances)];
            DashboardTotalsResponse totals = new(
                TotalsOf(allBalances.Where(balance => balance.MinorUnits > 0)),
                [.. TotalsOf(allBalances.Where(balance => balance.MinorUnits < 0)).Select(total => total with { AmountMinor = -total.AmountMinor })]);
            List<DashboardGroupRow> groupRows = [.. unfinishedGroups.Select(group => new DashboardGroupRow(
                group.Id,
                group.Kind,
                group.Name,
                group.Emoji,
                group.Status,
                [.. balancesByGroup[group.Id].Select(CurrencyAmountRowOf)]))];
            List<NeedsYouItem> needsYou = [
                .. await ClosingNeedsYouItemsOfAsync(unfinishedGroups, cancellationToken),
                .. await PaymentNeedsYouItemsOfAsync(unfinishedGroups, userId.Value, cancellationToken)];

            return Result.Ok(new DashboardResponse(
                totals,
                groupRows,
                needsYou,
                await DashboardActivityRowsOfAsync(memberGroups, cancellationToken),
                await PlusTargetOfAsync(unfinishedGroups, userId.Value, cancellationToken)));
        }

        private static List<CurrencyAmountRow> TotalsOf(IEnumerable<Money> balances)
        {
            return [.. balances
                .GroupBy(balance => balance.Currency)
                .OrderBy(currencyBalances => currencyBalances.Key)
                .Select(currencyBalances => new CurrencyAmountRow(currencyBalances.Key, currencyBalances.Sum(balance => balance.MinorUnits)))];
        }

        private static CurrencyAmountRow CurrencyAmountRowOf(Money amount)
        {
            return new CurrencyAmountRow(amount.Currency, amount.MinorUnits);
        }

        private async Task<Dictionary<Guid, IReadOnlyList<Money>>> CallerBalancesOfAsync(IReadOnlyList<DashboardGroupDto> groups, CancellationToken cancellationToken)
        {
            Dictionary<Guid, GroupMoney> moneyByGroup = await _context.GroupMoneyOfGroupsAsync([.. groups.Select(group => group.Id)], cancellationToken);

            return groups.ToDictionary(group => group.Id, group => moneyByGroup[group.Id].NonZeroBalancesOf(group.CallerMemberId));
        }

        private async Task<List<ClosingNeedsYouItem>> ClosingNeedsYouItemsOfAsync(IReadOnlyList<DashboardGroupDto> groups, CancellationToken cancellationToken)
        {
            List<DashboardGroupDto> closingGroups = [.. groups.Where(group => group.Status == GroupStatus.Closing)];
            List<Guid> closingGroupIds = [.. closingGroups.Select(group => group.Id)];
            HashSet<Guid> confirmedMemberIds = [.. await _context.ClosingResponses
                .AsNoTracking()
                .Where(response => closingGroupIds.Contains(response.GroupId) && response.Response == ClosingAnswer.Confirmed)
                .Select(response => response.MemberId)
                .ToListAsync(cancellationToken)];
            Dictionary<Guid, GroupRoster> rostersByGroup = await RostersOfAsync(closingGroupIds, cancellationToken);

            return [.. closingGroups
                .Where(group => rostersByGroup[group.Id].ClosingConfirmers(group.OwnerUserId)
                    .Any(row => row.Member.Id == group.CallerMemberId && !confirmedMemberIds.Contains(row.Member.Id)))
                .Select(group => new ClosingNeedsYouItem(group.Id, group.Name, group.OwnerName))];
        }

        private async Task<List<PaymentNeedsYouItem>> PaymentNeedsYouItemsOfAsync(IReadOnlyList<DashboardGroupDto> groups, Guid userId, CancellationToken cancellationToken)
        {
            Dictionary<Guid, DashboardGroupDto> groupsById = groups.ToDictionary(group => group.Id);
            List<Guid> groupIds = [.. groupsById.Keys];
            List<Settlement> pendingSettlements = await _context.Settlements
                .AsNoTracking()
                .Where(settlement => groupIds.Contains(settlement.GroupId) && settlement.Status == SettlementStatus.Pending && settlement.DeletedAt == null)
                .OrderBy(settlement => settlement.CreatedAt)
                .ThenBy(settlement => settlement.Id)
                .ToListAsync(cancellationToken);
            Dictionary<Guid, GroupRoster> rostersByGroup = await RostersOfAsync([.. pendingSettlements.Select(settlement => settlement.GroupId).Distinct()], cancellationToken);

            return [.. pendingSettlements
                .Where(settlement => settlement.CanBeAnsweredBy(rostersByGroup[settlement.GroupId], userId, groupsById[settlement.GroupId].OwnerUserId))
                .Select(settlement => new PaymentNeedsYouItem(
                    settlement.GroupId,
                    groupsById[settlement.GroupId].Name,
                    settlement.Id,
                    rostersByGroup[settlement.GroupId].RowOf(settlement.FromMemberId).DisplayName,
                    settlement.AmountMinor,
                    settlement.Currency))];
        }

        private async Task<Dictionary<Guid, GroupRoster>> RostersOfAsync(IReadOnlyCollection<Guid> groupIds, CancellationToken cancellationToken)
        {
            List<NamedMember> memberRows = await _context.NamedGroupMembers(groupIds).AsNoTracking().ToListAsync(cancellationToken);

            return memberRows
                .GroupBy(row => row.Member.GroupId)
                .ToDictionary(groupRows => groupRows.Key, groupRows => new GroupRoster([.. groupRows]));
        }

        private async Task<List<DashboardActivityRow>> DashboardActivityRowsOfAsync(IReadOnlyList<DashboardGroupDto> groups, CancellationToken cancellationToken)
        {
            Dictionary<Guid, string> groupNamesById = groups.ToDictionary(group => group.Id, group => group.Name);
            List<Guid> groupIds = [.. groupNamesById.Keys];
            List<ActivityEventDto> events = await _context
                .NewestActivityEventDtosOf(_context.ActivityEvents.Where(activityEvent => groupIds.Contains(activityEvent.GroupId)))
                .Take(RecentActivityLimit)
                .ToListAsync(cancellationToken);

            return [.. events.Select(activityEvent => new DashboardActivityRow(
                activityEvent.Id,
                activityEvent.Type,
                activityEvent.ActorUserId,
                activityEvent.ActorName,
                activityEvent.ExpenseId,
                activityEvent.MemberId,
                ActivityJson.Parse(activityEvent.Changes),
                ActivityJson.Parse(activityEvent.Data),
                activityEvent.CreatedAt,
                activityEvent.GroupId,
                groupNamesById[activityEvent.GroupId]))];
        }

        private async Task<PlusTargetResponse?> PlusTargetOfAsync(IReadOnlyList<DashboardGroupDto> groups, Guid userId, CancellationToken cancellationToken)
        {
            List<DashboardGroupDto> openNormalGroups = [.. groups.Where(group => group.Kind == GroupKind.Group && group.Status == GroupStatus.Open)];
            List<Guid> openNormalGroupIds = [.. openNormalGroups.Select(group => group.Id)];
            Guid? lastExpenseGroupId = await _context.Expenses
                .AsNoTracking()
                .Where(expense => openNormalGroupIds.Contains(expense.GroupId) && expense.CreatedByUserId == userId && expense.DeletedAt == null)
                .OrderByDescending(expense => expense.CreatedAt)
                .ThenByDescending(expense => expense.Id)
                .Select(expense => (Guid?)expense.GroupId)
                .FirstOrDefaultAsync(cancellationToken);
            Guid? targetGroupId = lastExpenseGroupId ?? openNormalGroups
                .OrderByDescending(group => group.CallerJoinedAt)
                .ThenByDescending(group => group.CallerMemberId)
                .Select(group => (Guid?)group.Id)
                .FirstOrDefault();

            return targetGroupId is Guid groupId ? new PlusTargetResponse(groupId) : null;
        }

        private IQueryable<DashboardGroupDto> DashboardGroupDtosOf(Guid userId)
        {
            return _context.CurrentGroupMembers()
                .AsNoTracking()
                .Where(member => member.UserId == userId)
                .Join(_context.Groups.Where(group => group.DeletedAt == null), member => member.GroupId, group => group.Id, (member, group) => new { Member = member, Group = group })
                .Join(_context.Users, row => row.Group.OwnerUserId, owner => owner.Id, (row, owner) => new { row.Member, row.Group, OwnerName = owner.DisplayName })
                .OrderByDescending(row => row.Group.CreatedAt)
                .ThenByDescending(row => row.Group.Id)
                .Select(row => new DashboardGroupDto(
                    row.Group.Id,
                    row.Group.Kind,
                    row.Group.Name,
                    row.Group.Emoji,
                    row.Group.Status,
                    row.Group.ClosingStartedAt,
                    row.Group.OwnerUserId,
                    row.OwnerName,
                    row.Member.Id,
                    row.Member.JoinedAt));
        }
    }
}
