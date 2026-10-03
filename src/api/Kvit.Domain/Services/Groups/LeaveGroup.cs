using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class LeaveGroup(
        IGroupRepository _groups,
        IGroupMemberRepository _members,
        IActivityEventRepository _activityEvents,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid groupId, Guid userId, CancellationToken cancellationToken)
        {
            Result<Group> found = await _groups.FindForMemberAsync(groupId, userId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            GroupRoster roster = await _members.RosterOfAsync(groupId, cancellationToken);
            NamedMember ownRow = roster.CurrentRowOf(userId)
                ?? throw new InvalidOperationException($"User {userId} was found as a current member of group {groupId}, but has no current row in it.");

            DateTimeOffset now = _timeProvider.GetUtcNow();
            Result left = ownRow.Member.Leave(found.Value.OwnerUserId, now);
            if (!left.IsSuccess)
            {
                return left;
            }

            _activityEvents.Add(ActivityEvent.MemberLeft(groupId, userId, ownRow.Member.Id, ownRow.DisplayName, now));

            return Result.Ok();
        }
    }
}
