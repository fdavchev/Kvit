using Kvit.Domain.Expenses;

namespace Kvit.Application.Commands.Expenses
{
    public sealed record AddExpenseCommand(Guid GroupId, Guid ClientRequestId, ExpenseInput Input);
}
