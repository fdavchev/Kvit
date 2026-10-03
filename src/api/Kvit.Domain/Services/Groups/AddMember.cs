using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class AddMember(
        IGroupRepository _groups,
        IGroupMemberRepository _members,
        IActivityEventRepository _activityEvents,
        TimeProvider _timeProvider)
    {
        public async Task<Result<Guid>> Execute(Guid groupId, Guid userId, string name, CancellationToken cancellationToken)
        {
            Result<Group> found = await _groups.FindForMemberAsync(groupId, userId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found.ToFailure<Guid>();
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            Result<GroupMember> created = GroupMember.ForName(groupId, name, userId, now);
            if (!created.IsSuccess)
            {
                return created.ToFailure<Guid>();
            }

            GroupMember member = created.Value;
            GroupRoster roster = await _members.RosterOfAsync(groupId, cancellationToken);
            Result nameFree = roster.CheckNameFree(member.Name);
            if (!nameFree.IsSuccess)
            {
                return nameFree.ToFailure<Guid>();
            }

            _members.Add(member);
            _activityEvents.Add(ActivityEvent.MemberAdded(groupId, userId, member.Id, member.Name, now));

            return Result.Ok(member.Id);
        }
    }
}
