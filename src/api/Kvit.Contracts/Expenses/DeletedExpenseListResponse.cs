namespace Kvit.Contracts.Expenses
{
    public sealed record DeletedExpenseListResponse(IReadOnlyList<DeletedExpenseListRow> Expenses);
}
