namespace Kvit.Domain.PersonalEntries
{
    public sealed record RecurringIncomeInput(
        long AmountMinor,
        string Currency,
        string? Title,
        string Frequency,
        int? DayOfMonth,
        int? DayOfWeek,
        string StartsOn,
        string? EndsOn);
}
