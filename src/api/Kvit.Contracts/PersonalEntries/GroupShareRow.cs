using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.PersonalEntries
{
    public sealed record GroupShareRow(
        Guid ExpenseId,
        Guid GroupId,
        string GroupName,
        string? Title,
        Guid? CategoryId,
        DateOnly EntryDate,
        Currency Currency,
        long ShareMinor);
}
