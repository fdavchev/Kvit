using Kvit.Contracts.Validation;

namespace Kvit.Contracts.Expenses
{
    public sealed record OneBillRequest(
        [NotEmptyGuid] Guid ClientRequestId,
        string? Title,
        string? GroupName,
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
