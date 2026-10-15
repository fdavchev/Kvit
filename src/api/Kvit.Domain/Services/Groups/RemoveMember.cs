using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class RemoveMember(
        IGroupRepository _groups,
        IGroupMemberRepository _members,
        IGroupMoneyRepository _money,
        IActivityEventRepository _activityEvents,
        CancelClosingOnChange _cancelClosingOnChange,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid groupId, Guid userId, Guid memberId, CancellationToken cancellationToken)
        {
            Result<Group> found = await _groups.FindForOwnerAsync(groupId, userId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            Result changeable = await _cancelClosingOnChange.Execute(found.Value, userId, cancellationToken);
            if (!changeable.IsSuccess)
            {
                return changeable;
            }

            GroupRoster roster = await _members.RosterOfAsync(groupId, cancellationToken);
            Result<NamedMember> target = roster.FindCurrent(memberId);
            if (!target.IsSuccess)
            {
                return target;
            }

            Result canBeRemoved = target.Value.Member.CheckCanBeRemoved(found.Value.OwnerUserId);
            if (!canBeRemoved.IsSuccess)
            {
                return canBeRemoved;
            }

            GroupMoney money = await _money.MoneyOfAsync(groupId, cancellationToken);
            Result settled = money.CheckMemberSettled(memberId);
            if (!settled.IsSuccess)
            {
                return settled;
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            Result removed = target.Value.Member.Remove(found.Value.OwnerUserId, userId, now);
            if (!removed.IsSuccess)
            {
                return removed;
            }

            _activityEvents.Add(ActivityEvent.MemberRemoved(groupId, userId, memberId, target.Value.DisplayName, now));

            return Result.Ok();
        }
    }
}
