namespace Kvit.Contracts.Me
{
    public sealed record ExportedPersonalBudget(
        IReadOnlyList<ExportedCategory> Categories,
        IReadOnlyList<ExportedEntry> Entries,
        IReadOnlyList<ExportedRecurringIncome> RecurringIncomes,
        IReadOnlyList<ExportedBudgetLimit> BudgetLimits,
        IReadOnlyList<ExportedDuplicateAnswer> DuplicateAnswers);
}
