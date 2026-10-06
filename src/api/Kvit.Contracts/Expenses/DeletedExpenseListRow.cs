using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.Expenses
{
    public sealed record DeletedExpenseListRow(
        Guid Id,
        string? Title,
        long AmountMinor,
        Currency Currency,
        DateOnly ExpenseDate,
        Guid? CategoryId,
        string PaidByName,
        DateTimeOffset DeletedAt,
        DateTimeOffset RestorableUntil,
        bool CanEdit);
}
