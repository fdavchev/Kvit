namespace Kvit.Contracts.Expenses
{
    public sealed record OneBillRequest(
        Guid ClientRequestId,
        string? Title,
        string? Emoji,
        IReadOnlyList<string> Names,
        string? Note,
        long AmountMinor,
        string Currency,
        string ExpenseDate,
        Guid? CategoryId,
        int PaidByPersonIndex,
        string SplitType,
        IReadOnlyList<OneBillShareRequest> Shares);
}
