using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.Me
{
    public sealed record ExportedRecurringIncome(
        Guid Id,
        string? Title,
        long AmountMinor,
        Currency Currency,
        RecurringIncomeFrequency Frequency,
        int? DayOfMonth,
        int? DayOfWeek,
        DateOnly StartsOn,
        DateOnly? EndsOn,
        DateTimeOffset CreatedAt,
        DateTimeOffset? StoppedAt);
}
