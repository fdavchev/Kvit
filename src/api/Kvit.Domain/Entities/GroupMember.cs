using Kvit.Domain.Accounts;
using Kvit.Domain.Results;

namespace Kvit.Domain.Entities
{
    public class GroupMember
    {
        private GroupMember()
        {
        }

        public Guid Id { get; private set; }

        public Guid GroupId { get; private set; }

        public Guid? UserId { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public DateTimeOffset JoinedAt { get; private set; }

        public Guid AddedByUserId { get; private set; }

        public DateTimeOffset? ClaimedAt { get; private set; }

        public DateTimeOffset? RemovedAt { get; private set; }

        public Guid? RemovedByUserId { get; private set; }

        public MemberEndKind? EndKind { get; private set; }

        public static GroupMember ForAccount(Guid groupId, Guid userId, string accountName, Guid addedByUserId, DateTimeOffset joinedAt)
        {
            return new GroupMember
            {
                Id = Guid.CreateVersion7(),
                GroupId = groupId,
                UserId = userId,
                Name = accountName,
                JoinedAt = joinedAt,
                AddedByUserId = addedByUserId,
            };
        }

        public static Result<GroupMember> ForName(Guid groupId, string name, Guid addedByUserId, DateTimeOffset joinedAt)
        {
            Result<string> validName = AccountRules.ValidateDisplayName(name);
            if (!validName.IsSuccess)
            {
                return Result.Failure<GroupMember>($"The name must have 1 to {AccountRules.DisplayNameMaxLength} characters.", ResultCodes.MEMBER_NAME_INVALID);
            }

            return Result.Ok(new GroupMember
            {
                Id = Guid.CreateVersion7(),
                GroupId = groupId,
                Name = validName.Value,
                JoinedAt = joinedAt,
                AddedByUserId = addedByUserId,
            });
        }

        public static Result<T> NotFound<T>(Guid memberId)
        {
            return Result.NotFound<T>($"Member {memberId} is not in this group, or not in a state that allows this.", ResultCodes.MEMBER_NOT_FOUND);
        }

        public Result Remove(Guid ownerUserId, Guid byUserId, DateTimeOffset removedAt)
        {
            if (UserId == ownerUserId)
            {
                return Result.Failure($"Member {Id} is the owner of the group and cannot be removed.", ResultCodes.MEMBER_IS_OWNER);
            }

            End(MemberEndKind.Removed, byUserId, removedAt);

            return Result.Ok();
        }

        public Result Leave(Guid ownerUserId, DateTimeOffset leftAt)
        {
            Guid leaverUserId = AccountUserId();
            if (leaverUserId == ownerUserId)
            {
                return Result.Failure("The owner cannot leave the group. Make someone else the owner first.", ResultCodes.MEMBER_OWNER_CANNOT_LEAVE);
            }

            End(MemberEndKind.Left, leaverUserId, leftAt);

            return Result.Ok();
        }

        public void SetAside(DateTimeOffset setAsideAt)
        {
            End(MemberEndKind.SetAside, AccountUserId(), setAsideAt);
        }

        public void Reinstate()
        {
            RemovedAt = null;
            RemovedByUserId = null;
            EndKind = null;
        }

        public Result CheckCanClaimNames()
        {
            if (ClaimedAt is not null)
            {
                return Result.Failure($"Member {Id} already holds a claimed name, so it cannot claim another one.", ResultCodes.MEMBER_CANNOT_CLAIM);
            }

            return Result.Ok();
        }

        public void Claim(Guid userId, DateTimeOffset claimedAt)
        {
            UserId = userId;
            ClaimedAt = claimedAt;
        }

        public Result<Guid> UndoClaim()
        {
            if (ClaimedAt is null)
            {
                return Result.Failure<Guid>($"Member {Id} was never claimed, so there is no claim to undo.", ResultCodes.MEMBER_NOT_CLAIMED);
            }

            Guid claimerUserId = AccountUserId();
            UserId = null;
            ClaimedAt = null;

            return Result.Ok(claimerUserId);
        }

        private Guid AccountUserId()
        {
            return UserId ?? throw new InvalidOperationException($"Member {Id} of group {GroupId} is a plain name with no account, but this step needs the account.");
        }

        private void End(MemberEndKind endKind, Guid byUserId, DateTimeOffset endedAt)
        {
            RemovedAt = endedAt;
            RemovedByUserId = byUserId;
            EndKind = endKind;
        }
    }
}
