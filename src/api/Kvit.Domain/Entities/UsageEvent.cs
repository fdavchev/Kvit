namespace Kvit.Domain.Entities
{
    public class UsageEvent
    {
        private UsageEvent()
        {
        }

        public long Id { get; private set; }

        public Guid? UserId { get; private set; }

        public UsageEventType Type { get; private set; }

        public string? Detail { get; private set; }

        public DateTimeOffset OccurredAt { get; private set; }

        public DateOnly OccurredOn { get; private set; }

        public static UsageEvent SignedUp(Guid userId, SignUpMethod method, DateTimeOffset occurredAt)
        {
            return New(userId, UsageEventType.SignedUp, method.ToString(), occurredAt);
        }

        public static UsageEvent GuestUpgraded(Guid userId, SignUpMethod method, DateTimeOffset occurredAt)
        {
            return New(userId, UsageEventType.GuestUpgraded, method.ToString(), occurredAt);
        }

        public static UsageEvent GroupCreated(Guid userId, DateTimeOffset occurredAt)
        {
            return New(userId, UsageEventType.GroupCreated, null, occurredAt);
        }

        public static UsageEvent JoinedViaInvite(Guid userId, DateTimeOffset occurredAt)
        {
            return New(userId, UsageEventType.JoinedViaInvite, null, occurredAt);
        }

        public static UsageEvent ExpenseAdded(Guid userId, DateTimeOffset occurredAt)
        {
            return New(userId, UsageEventType.ExpenseAdded, null, occurredAt);
        }

        public static UsageEvent SettlementConfirmed(Guid userId, DateTimeOffset occurredAt)
        {
            return New(userId, UsageEventType.SettlementConfirmed, null, occurredAt);
        }

        public static UsageEvent Active(Guid userId, DateTimeOffset occurredAt)
        {
            return New(userId, UsageEventType.Active, null, occurredAt);
        }

        public static DateOnly UtcDayOf(DateTimeOffset moment)
        {
            return DateOnly.FromDateTime(moment.UtcDateTime);
        }

        private static UsageEvent New(Guid userId, UsageEventType type, string? detail, DateTimeOffset occurredAt)
        {
            DateTimeOffset occurredAtUtc = occurredAt.ToUniversalTime();

            return new UsageEvent
            {
                UserId = userId,
                Type = type,
                Detail = detail,
                OccurredAt = occurredAtUtc,
                OccurredOn = UtcDayOf(occurredAtUtc),
            };
        }
    }
}
