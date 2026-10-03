using Kvit.Domain.Entities;
using Kvit.Domain.Results;

namespace Kvit.Domain.Tests.Entities
{
    public static class GroupTestData
    {
        public const string ValidName = "Greece trip";
        public const string ValidEmoji = "\U0001F3D6";
        public const string ValidCurrency = "MKD";
        public const string InviteToken = "invite-token-1";

        public static readonly Guid OwnerId = Guid.Parse("0199a1b2-c3d4-7e5f-8a6b-7c8d9e0f1a2b");
        public static readonly Guid MemberId = Guid.Parse("0199a1b2-c3d4-7e5f-8a6b-7c8d9e0f1a2c");
        public static readonly Guid StrangerId = Guid.Parse("0199a1b2-c3d4-7e5f-8a6b-7c8d9e0f1a2d");
        public static readonly DateTimeOffset Moment = new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);

        public static Group NewGroup()
        {
            Result<Group> created = Group.Create(ValidName, ValidEmoji, ValidCurrency, OwnerId, InviteToken, Moment);

            return created.Value;
        }

        public static Group NewDeletedGroup(DateTimeOffset deletedAt)
        {
            Group group = NewGroup();
            group.Delete(OwnerId, deletedAt);

            return group;
        }
    }
}
