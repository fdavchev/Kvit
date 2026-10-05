using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class LetBackIn(
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
            Result<NamedMember> target = roster.FindRemoved(memberId);
            if (!target.IsSuccess)
            {
                return target;
            }

            GroupMember member = target.Value.Member;
            if (member.UserId is null)
            {
                Result nameFree = roster.CheckNameFree(member.Name);
                if (!nameFree.IsSuccess)
                {
                    return nameFree;
                }
            }

            member.Reinstate();
            _activityEvents.Add(ActivityEvent.MemberLetBackIn(groupId, userId, memberId, target.Value.DisplayName, _timeProvider.GetUtcNow()));

            return Result.Ok();
        }
    }
}
