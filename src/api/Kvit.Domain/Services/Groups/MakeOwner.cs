using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class MakeOwner(
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

            Result transferred = found.Value.TransferOwnership(target.Value.Member);
            if (!transferred.IsSuccess)
            {
                return transferred;
            }

            _activityEvents.Add(ActivityEvent.OwnershipTransferred(groupId, userId, memberId, target.Value.DisplayName, _timeProvider.GetUtcNow()));

            return Result.Ok();
        }
    }
}
