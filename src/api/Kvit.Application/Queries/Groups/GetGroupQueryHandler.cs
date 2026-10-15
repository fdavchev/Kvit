using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Groups;
using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.Groups
{
    public sealed class GetGroupQueryHandler(
        ICurrentUserProvider _currentUserProvider,
        AppDbContext _context,
        TimeProvider _timeProvider) : IQueryHandler<GetGroupQuery, GroupResponse>
    {
        public async Task<Result<GroupResponse>> Handle(GetGroupQuery query, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return Result.Unauthorized<GroupResponse>(userId.Error, userId.ErrorCode);
            }

            GroupDetailDto? detail = await GroupDetailDtoOf(query.GroupId, userId.Value).SingleOrDefaultAsync(cancellationToken);
            if (detail is null)
            {
                return Group.NotFound<GroupResponse>(query.GroupId);
            }

            Group group = detail.Group;
            DateTimeOffset now = _timeProvider.GetUtcNow();
            GroupStatus status = group.EffectiveStatusAt(now);
            List<ClosingMemberRow> confirmedBy = [];
            List<ClosingMemberRow> waitingFor = [];
            if (status != GroupStatus.Open)
            {
                GroupRoster roster = new(await _context.NamedGroupMembers(group.Id).AsNoTracking().ToListAsync(cancellationToken));
                HashSet<Guid> confirmedMemberIds = [.. await ConfirmedMemberIdsOf(group.Id).ToListAsync(cancellationToken)];
                confirmedBy = [.. roster.InJoiningOrder(confirmedMemberIds, memberId => memberId).Select(memberId => ClosingMemberRowOf(roster.RowOf(memberId)))];
                if (status == GroupStatus.Closing)
                {
                    waitingFor = [.. roster.ClosingConfirmers(group.OwnerUserId)
                        .Where(row => !confirmedMemberIds.Contains(row.Member.Id))
                        .Select(ClosingMemberRowOf)];
                }
            }

            return Result.Ok(new GroupResponse(
                group.Id,
                group.Kind,
                group.Name,
                group.Emoji,
                group.DefaultCurrency,
                status,
                group.OwnerUserId,
                group.OwnerUserId == userId.Value,
                detail.MemberCount,
                group.InviteToken,
                group.ClosingStartedAt,
                Group.ClosingEndsAtOf(group.ClosingStartedAt),
                confirmedBy,
                waitingFor,
                await CanStartClosingAsync(group, userId.Value, now, cancellationToken)));
        }

        private static ClosingMemberRow ClosingMemberRowOf(NamedMember row)
        {
            return new ClosingMemberRow(row.Member.Id, row.DisplayName);
        }

        private async Task<bool> CanStartClosingAsync(Group group, Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            if (!group.CheckCanStartClosing(userId, now).IsSuccess)
            {
                return false;
            }

            GroupMoney money = await _context.GroupMoneyOfAsync(group.Id, cancellationToken);

            return money.CheckHasCountedExpense().IsSuccess && money.CheckEveryoneKvit().IsSuccess;
        }

        private IQueryable<Guid> ConfirmedMemberIdsOf(Guid groupId)
        {
            return _context.ClosingResponses
                .AsNoTracking()
                .Where(response => response.GroupId == groupId && response.Response == ClosingAnswer.Confirmed)
                .Select(response => response.MemberId);
        }

        private IQueryable<GroupDetailDto> GroupDetailDtoOf(Guid groupId, Guid userId)
        {
            IQueryable<GroupMember> currentMembers = _context.CurrentGroupMembers();

            return _context.GroupsWithCurrentMember(userId)
                .AsNoTracking()
                .Where(group => group.Id == groupId && group.DeletedAt == null)
                .Select(group => new GroupDetailDto(group, currentMembers.Count(member => member.GroupId == group.Id)));
        }
    }
}
