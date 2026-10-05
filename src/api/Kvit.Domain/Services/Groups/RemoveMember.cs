using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class RemoveMember(
        IGroupRepository _groups,
        IGroupMemberRepository _members,
        IActivityEventRepository _activityEvents,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid groupId, Guid userId, Guid memberId, CancellationToken cancellationToken)
        {
            Result<Group> found = await _groups.FindForOwnerAsync(groupId, userId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            GroupRoster roster = await _members.RosterOfAsync(groupId, cancellationToken);
            Result<NamedMember> target = roster.FindCurrent(memberId);
            if (!target.IsSuccess)
            {
                return target;
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
