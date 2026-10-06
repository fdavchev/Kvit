namespace Kvit.Application.Queries.Expenses
{
    public sealed record GetExpenseQuery(Guid GroupId, Guid ExpenseId);
}
