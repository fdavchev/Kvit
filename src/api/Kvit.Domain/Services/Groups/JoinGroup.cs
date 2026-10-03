using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class JoinGroup(
        IGroupRepository _groups,
        IGroupMemberRepository _members,
        IActivityEventRepository _activityEvents,
        IUsageEventRepository _usageEvents,
        TimeProvider _timeProvider)
    {
        public async Task<Result<Guid>> Execute(string inviteToken, Guid userId, string accountName, Guid? claimMemberId, CancellationToken cancellationToken)
        {
            Result<Group> found = await _groups.FindByInviteTokenAsync(inviteToken, cancellationToken);
            if (!found.IsSuccess)
            {
                return found.ToFailure<Guid>();
            }

            Guid groupId = found.Value.Id;
            GroupRoster roster = await _members.RosterOfAsync(groupId, cancellationToken);
            if (roster.CurrentRowOf(userId) is not null)
            {
                return Result.Ok(groupId);
            }

            if (roster.LatestEndedRowOf(userId, MemberEndKind.Removed) is not null)
            {
                return Result.Forbid<Guid>($"You were removed from group {groupId}. Ask the owner to let you back in.", ResultCodes.INVITE_REMOVED);
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            if (claimMemberId is Guid targetId)
            {
                Result claimed = ClaimAtJoin(roster, groupId, userId, accountName, targetId, now);
                if (!claimed.IsSuccess)
                {
                    return claimed.ToFailure<Guid>();
                }
            }
            else
            {
                JoinOwnRow(roster, groupId, userId, accountName, now);
            }

            _usageEvents.Add(UsageEvent.JoinedViaInvite(userId, now));

            return Result.Ok(groupId);
        }

        private Result ClaimAtJoin(GroupRoster roster, Guid groupId, Guid userId, string accountName, Guid targetId, DateTimeOffset now)
        {
            Result<NamedMember> target = roster.FindCurrentPlainName(targetId);
            if (!target.IsSuccess)
            {
                return target;
            }

            if (roster.HasAnyRowOf(userId))
            {
                return Result.Failure($"User {userId} already has a row in group {groupId}, so they come back on it and cannot claim a name while joining.", ResultCodes.MEMBER_CANNOT_CLAIM);
            }

            GroupMember member = target.Value.Member;
            member.Claim(userId, now);
            _activityEvents.Add(ActivityEvent.MemberJoined(groupId, userId, member.Id, accountName, now));
            _activityEvents.Add(ActivityEvent.MemberClaimed(groupId, userId, member.Id, accountName, member.Name, now));

            return Result.Ok();
        }

        private void JoinOwnRow(GroupRoster roster, Guid groupId, Guid userId, string accountName, DateTimeOffset now)
        {
            NamedMember? leftRow = roster.LatestEndedRowOf(userId, MemberEndKind.Left);
            GroupMember member;
            if (leftRow is null)
            {
                member = GroupMember.ForAccount(groupId, userId, accountName, userId, now);
                _members.Add(member);
            }
            else
            {
                member = leftRow.Member;
                member.Reinstate();
            }

            _activityEvents.Add(ActivityEvent.MemberJoined(groupId, userId, member.Id, accountName, now));
        }
    }
}
