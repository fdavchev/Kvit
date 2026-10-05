using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class UndoClaim(
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

            Result<Guid> undone = target.Value.Member.UndoClaim();
            if (!undone.IsSuccess)
            {
                return undone;
            }

            Guid claimerUserId = undone.Value;
            string claimerName = target.Value.DisplayName;
            DateTimeOffset now = _timeProvider.GetUtcNow();
            await _members.SaveAsync(cancellationToken);

            NamedMember? setAsideRow = roster.LatestEndedRowOf(claimerUserId, MemberEndKind.SetAside);
            if (setAsideRow is null)
            {
                _members.Add(GroupMember.ForAccount(groupId, claimerUserId, claimerName, userId, now));
            }
            else
            {
                setAsideRow.Member.Reinstate();
            }

            _activityEvents.Add(ActivityEvent.ClaimUndone(groupId, userId, memberId, claimerName, target.Value.Member.Name, now));

            return Result.Ok();
        }
    }
}
