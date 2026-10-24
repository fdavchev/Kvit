using Kvit.Contracts.Validation;

namespace Kvit.Contracts.RecurringIncomes
{
    public sealed record CreateRecurringIncomeRequest(
        [NotEmptyGuid] Guid ClientRequestId,
        long AmountMinor,
        string Currency,
        string? Title,
        string Frequency,
        int? DayOfMonth,
        int? DayOfWeek,
        string StartsOn,
        string? EndsOn);
}
