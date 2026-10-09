namespace Kvit.Domain.Expenses
{
    public sealed record OneBillInput(
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
        IReadOnlyList<PersonShareInput> Shares);
}
