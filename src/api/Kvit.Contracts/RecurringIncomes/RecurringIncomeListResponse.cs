namespace Kvit.Contracts.RecurringIncomes
{
    public sealed record RecurringIncomeListResponse(IReadOnlyList<RecurringIncomeRow> RecurringIncomes);
}
