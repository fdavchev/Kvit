namespace Kvit.Contracts.RecurringIncomes
{
    public sealed record EditRecurringIncomeRequest(
        long AmountMinor,
        string Currency,
        string? Title,
        string Frequency,
        int? DayOfMonth,
        int? DayOfWeek,
        string StartsOn,
        string? EndsOn);
}
