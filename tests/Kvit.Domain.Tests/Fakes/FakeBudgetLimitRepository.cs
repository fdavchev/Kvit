using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;

namespace Kvit.Domain.Tests.Fakes
{
    public sealed class FakeBudgetLimitRepository : IBudgetLimitRepository
    {
        private readonly List<BudgetLimit> _rows = [];

        public List<BudgetLimit> Added { get; } = [];

        public List<BudgetLimit> Removed { get; } = [];

        public void Seed(params BudgetLimit[] rows)
        {
            _rows.AddRange(rows);
        }

        public void Add(BudgetLimit limit)
        {
            Added.Add(limit);
        }

        public void Remove(BudgetLimit limit)
        {
            Removed.Add(limit);
        }

        public Task<BudgetLimitHistory> HistoryOfAsync(Guid userId, Guid? categoryId, CancellationToken cancellationToken)
        {
            return Task.FromResult(new BudgetLimitHistory([.. _rows.Where(row => row.UserId == userId && row.CategoryId == categoryId)]));
        }
    }
}
