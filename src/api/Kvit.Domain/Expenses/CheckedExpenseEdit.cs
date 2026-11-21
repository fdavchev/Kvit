using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;

namespace Kvit.Domain.Expenses
{
    public sealed record CheckedExpenseEdit(
        Group Group,
        Expense Expense,
        ExpenseFields Fields,
        GroupRoster Roster,
        IReadOnlyList<SplitShare> Shares,
        IReadOnlyList<FieldChange> Changes)
    {
        public bool ChangesCurrency => Fields.Amount.Currency != Expense.Currency;
    }
}
