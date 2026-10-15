using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class StartClosing(
        IGroupRepository _groups,
        IGroupMemberRepository _members,
        IGroupMoneyRepository _money,
        IActivityEventRepository _activityEvents,
        FinishExpiredClosing _finishExpiredClosing,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid groupId, Guid userId, CancellationToken cancellationToken)
        {
            Result<Group> found = await _groups.FindForMemberAsync(groupId, userId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            Group group = found.Value;
            DateTimeOffset now = _timeProvider.GetUtcNow();
            _finishExpiredClosing.Execute(group, now);
            Result canStart = group.CheckCanStartClosing(userId, now);
            if (!canStart.IsSuccess)
            {
                return canStart;
            }

            GroupMoney money = await _money.MoneyOfAsync(groupId, cancellationToken);
            Result moneyAllowsClosing = money.CheckCanStartClosing();
            if (!moneyAllowsClosing.IsSuccess)
            {
                return moneyAllowsClosing;
            }

            group.StartClosing(now);
            _activityEvents.Add(ActivityEvent.ClosingStarted(groupId, userId, now));

            GroupRoster roster = await _members.RosterOfAsync(groupId, cancellationToken);
            if (roster.ClosingConfirmers(group.OwnerUserId).Count == 0)
            {
                group.Finish(now);
                _activityEvents.Add(ActivityEvent.GroupFinished(groupId, userId, now));
            }

            return Result.Ok();
        }
    }
}
