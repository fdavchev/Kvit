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
            DateTimeOffset occurredAtUtc = occurredAt.ToUniversalTime();

            return new UsageEvent
            {
                UserId = userId,
                Type = UsageEventType.SignedUp,
                Detail = method.ToString(),
                OccurredAt = occurredAtUtc,
                OccurredOn = DateOnly.FromDateTime(occurredAtUtc.UtcDateTime),
            };
        }
    }
}
