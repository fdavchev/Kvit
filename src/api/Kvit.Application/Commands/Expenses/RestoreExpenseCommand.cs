namespace Kvit.Application.Commands.Expenses
{
    public sealed record RestoreExpenseCommand(Guid GroupId, Guid ExpenseId);
}
