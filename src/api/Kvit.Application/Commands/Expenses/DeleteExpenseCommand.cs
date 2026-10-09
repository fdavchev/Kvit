namespace Kvit.Application.Commands.Expenses
{
    public sealed record DeleteExpenseCommand(Guid GroupId, Guid ExpenseId);
}
