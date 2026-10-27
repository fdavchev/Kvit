namespace Kvit.Domain.Entities
{
    public sealed class BudgetLimitHistory(IReadOnlyList<BudgetLimit> _rows)
    {
        public BudgetLimit? RowOf(DateOnly firstDayOfMonth)
        {
            return _rows.SingleOrDefault(row => row.EffectiveFrom == firstDayOfMonth);
        }

        public bool HasLimitInForceBefore(DateOnly firstDayOfMonth)
        {
            BudgetLimit? latestEarlier = _rows
                .Where(row => row.EffectiveFrom < firstDayOfMonth)
                .MaxBy(row => row.EffectiveFrom);

            return latestEarlier?.LimitMinor is not null;
        }
    }
}
