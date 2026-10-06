using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.Expenses
{
    public sealed record ExpenseListRow(
        Guid Id,
        string? Title,
        long AmountMinor,
        Currency Currency,
        DateOnly ExpenseDate,
        Guid? CategoryId,
        Guid PaidByMemberId,
        string PaidByName,
        long? YourShareMinor,
        bool CanEdit);
}
