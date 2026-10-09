using Kvit.Contracts.Validation;

namespace Kvit.Contracts.Expenses
{
    public sealed record AddExpenseRequest(
        [NotEmptyGuid] Guid ClientRequestId,
        string? Title,
        string? Note,
        long AmountMinor,
        string Currency,
        string ExpenseDate,
        Guid? CategoryId,
        Guid PaidByMemberId,
        string SplitType,
        IReadOnlyList<ShareRequest> Shares);
}
