namespace Kvit.Api.Tests.Expenses
{
    public sealed record OneBillShareInput(int PersonIndex, long InputValue);

    public sealed record OneBillInput(
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
        IReadOnlyList<OneBillShareInput> Shares)
    {
        public static OneBillInput EqualAmongEveryone(long amountMinor, string currency, params string[] names)
        {
            OneBillShareInput[] shares = [.. Enumerable.Range(0, names.Length + 1).Select(personIndex => new OneBillShareInput(personIndex, 0))];

            return new OneBillInput(null, null, names, null, amountMinor, currency, ExpenseInputs.DefaultDate, null, 0, "Equal", shares);
        }
    }
}
