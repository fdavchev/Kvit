using Kvit.Domain.Expenses;

namespace Kvit.Application.Commands.Expenses
{
    public sealed record EditExpenseCommand(Guid GroupId, Guid ExpenseId, ExpenseInput Input);
}
