using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.RecurringIncomes
{
    public sealed record RecurringIncomeRow(
        Guid Id,
        long AmountMinor,
        Currency Currency,
        string? Title,
        RecurringIncomeFrequency Frequency,
        int? DayOfMonth,
        int? DayOfWeek,
        DateOnly StartsOn,
        DateOnly? EndsOn,
        DateTimeOffset CreatedAt,
        DateOnly? NextDate,
        bool HasEntries);
}
