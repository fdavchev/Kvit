using Kvit.Domain.MoneyRules;

namespace Kvit.Domain.Entities
{
    public class RecurringIncome
    {
        public const int LastDayOfMonth = 31;
        public const int LastDayOfWeek = 6;

        private RecurringIncome()
        {
        }

        public Guid Id { get; private set; }

        public Guid UserId { get; private set; }

        public long AmountMinor { get; private set; }

        public Currency Currency { get; private set; }

        public string? Title { get; private set; }

        public RecurringIncomeFrequency Frequency { get; private set; }

        public int? DayOfMonth { get; private set; }

        public int? DayOfWeek { get; private set; }

        public DateOnly StartsOn { get; private set; }

        public DateOnly? EndsOn { get; private set; }

        public Guid ClientRequestId { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public DateTimeOffset? StoppedAt { get; private set; }
    }
}
