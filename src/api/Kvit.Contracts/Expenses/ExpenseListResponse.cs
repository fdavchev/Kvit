namespace Kvit.Contracts.Expenses
{
    public sealed record ExpenseListResponse(IReadOnlyList<ExpenseListRow> Expenses);
}
