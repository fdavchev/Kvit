using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;

namespace Kvit.Domain.Entities
{
    public class Group
    {
        public const int DeletedGroupRestoreDays = 30;

        private Group()
        {
        }

        public Guid Id { get; private set; }

        public GroupKind Kind { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public string Emoji { get; private set; } = string.Empty;

        public Currency DefaultCurrency { get; private set; }

        public GroupStatus Status { get; private set; }

        public Guid OwnerUserId { get; private set; }

        public DateTimeOffset? ClosingStartedAt { get; private set; }

        public DateTimeOffset? FinishedAt { get; private set; }

        public string InviteToken { get; private set; } = string.Empty;

        public DateTimeOffset InviteTokenCreatedAt { get; private set; }

        public string? PreviousInviteToken { get; private set; }

        public Guid CreatedByUserId { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public DateTimeOffset? DeletedAt { get; private set; }

        public Guid? DeletedByUserId { get; private set; }

        public static Result<Group> Create(string name, string emoji, string currency, Guid ownerUserId, string inviteToken, DateTimeOffset createdAt)
        {
            Result<GroupSettings> settings = GroupSettings.Create(name, emoji, currency);
            if (!settings.IsSuccess)
            {
                return Result.Failure<Group>(settings.Error, settings.ErrorCode);
            }

            return Result.Ok(new Group
            {
                Id = Guid.CreateVersion7(),
                Kind = GroupKind.Group,
                Name = settings.Value.Name,
                Emoji = settings.Value.Emoji,
                DefaultCurrency = settings.Value.DefaultCurrency,
                Status = GroupStatus.Open,
                OwnerUserId = ownerUserId,
                InviteToken = inviteToken,
                InviteTokenCreatedAt = createdAt,
                CreatedByUserId = ownerUserId,
                CreatedAt = createdAt,
            });
        }

        public static Result<T> NotFound<T>(Guid groupId)
        {
            return Result.NotFound<T>($"Group {groupId} does not exist, or you are not in it.", ResultCodes.GROUP_NOT_FOUND);
        }

        public static Result<T> InviteNotFound<T>()
        {
            return Result.NotFound<T>("This invite link does not lead to any group. It may have been reset, or the group was deleted.", ResultCodes.INVITE_NOT_FOUND);
        }

        public static DateTimeOffset RestorableUntil(DateTimeOffset deletedAt)
        {
            return deletedAt.AddDays(DeletedGroupRestoreDays);
        }

        public Result CheckOwner(Guid userId)
        {
            if (OwnerUserId != userId)
            {
                return Result.Forbid($"Only the owner of group {Id} can do this.", ResultCodes.GROUP_NOT_OWNER);
            }

            return Result.Ok();
        }

        public Result<GroupChanges> Update(string name, string emoji, string currency)
        {
            Result<GroupSettings> settings = GroupSettings.Create(name, emoji, currency);
            if (!settings.IsSuccess)
            {
                return Result.Failure<GroupChanges>(settings.Error, settings.ErrorCode);
            }

            GroupSettings newSettings = settings.Value;
            FieldChange? nameChange = Name == newSettings.Name ? null : new FieldChange("name", Name, newSettings.Name);
            List<FieldChange> settingsChanges = [];
            if (Emoji != newSettings.Emoji)
            {
                settingsChanges.Add(new FieldChange("emoji", Emoji, newSettings.Emoji));
            }

            if (DefaultCurrency != newSettings.DefaultCurrency)
            {
                settingsChanges.Add(new FieldChange("defaultCurrency", DefaultCurrency.ToString(), newSettings.DefaultCurrency.ToString()));
            }

            Name = newSettings.Name;
            Emoji = newSettings.Emoji;
            DefaultCurrency = newSettings.DefaultCurrency;

            return Result.Ok(new GroupChanges(nameChange, settingsChanges));
        }

        public void ResetInvite(string newToken, DateTimeOffset resetAt)
        {
            PreviousInviteToken = InviteToken;
            InviteToken = newToken;
            InviteTokenCreatedAt = resetAt;
        }

        public Result UndoInviteReset(DateTimeOffset undoneAt)
        {
            if (PreviousInviteToken is not string previousToken)
            {
                return Result.Failure($"The invite link of group {Id} was not reset, so there is nothing to undo.", ResultCodes.INVITE_NOTHING_TO_UNDO);
            }

            InviteToken = previousToken;
            PreviousInviteToken = null;
            InviteTokenCreatedAt = undoneAt;

            return Result.Ok();
        }

        public Result TransferOwnership(GroupMember newOwner)
        {
            if (newOwner.UserId is not Guid newOwnerUserId)
            {
                return Result.Failure($"Member {newOwner.Id} is a plain name without an account and cannot become the owner.", ResultCodes.MEMBER_NOT_ACCOUNT);
            }

            if (newOwnerUserId == OwnerUserId)
            {
                return Result.Failure($"Member {newOwner.Id} is already the owner of group {Id}.", ResultCodes.MEMBER_ALREADY_OWNER);
            }

            OwnerUserId = newOwnerUserId;

            return Result.Ok();
        }

        public void Delete(Guid byUserId, DateTimeOffset deletedAt)
        {
            DeletedAt = deletedAt;
            DeletedByUserId = byUserId;
        }

        public Result Restore(DateTimeOffset now)
        {
            if (DeletedAt is not DateTimeOffset deletedAt)
            {
                return Result.Failure($"Group {Id} is not deleted.", ResultCodes.GROUP_NOT_DELETED);
            }

            if (now >= RestorableUntil(deletedAt))
            {
                return Result.Failure($"Group {Id} was deleted more than {DeletedGroupRestoreDays} days ago and can no longer be restored.", ResultCodes.GROUP_RESTORE_EXPIRED);
            }

            DeletedAt = null;
            DeletedByUserId = null;

            return Result.Ok();
        }
    }
}
