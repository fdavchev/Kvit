using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class ClaimName(
        IGroupRepository _groups,
        IGroupMemberRepository _members,
        IActivityEventRepository _activityEvents,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid groupId, Guid userId, Guid memberId, CancellationToken cancellationToken)
        {
            Result<Group> found = await _groups.FindForMemberAsync(groupId, userId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            Result changeable = found.Value.CheckCanBeChangedBy(userId, now);
            if (!changeable.IsSuccess)
            {
                return changeable;
            }

            GroupRoster roster = await _members.RosterOfAsync(groupId, cancellationToken);
            Result<NamedMember> target = roster.FindCurrentPlainName(memberId);
            if (!target.IsSuccess)
            {
                return target;
            }

            NamedMember ownRow = roster.CurrentRowOf(userId)
                ?? throw new InvalidOperationException($"User {userId} was found as a current member of group {groupId}, but has no current row in it.");
            Result canClaim = ownRow.Member.CheckCanClaimNames();
            if (!canClaim.IsSuccess)
            {
                return canClaim;
            }

            if (ownRow.IsInAnyExpenseOrSettlement)
            {
                return Result.Failure($"Member {ownRow.Member.Id} already has expenses or payments recorded under it, so it cannot claim another name.", ResultCodes.MEMBER_CANNOT_CLAIM);
            }

            ownRow.Member.SetAside(now);
            await _members.SaveAsync(cancellationToken);
            target.Value.Member.Claim(userId, now);
            _activityEvents.Add(ActivityEvent.MemberClaimed(groupId, userId, memberId, ownRow.DisplayName, target.Value.Member.Name, now));

            return Result.Ok();
        }
    }
}
