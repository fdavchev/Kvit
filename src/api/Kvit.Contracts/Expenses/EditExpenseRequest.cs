namespace Kvit.Contracts.Expenses
{
    public sealed record EditExpenseRequest(
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
