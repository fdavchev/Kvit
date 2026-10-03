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
    }
}
