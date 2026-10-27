using Kvit.Domain.Entities;

namespace Kvit.Domain.Interfaces
{
    public interface IBudgetLimitRepository
    {
        void Add(BudgetLimit limit);

        void Remove(BudgetLimit limit);

        Task<BudgetLimitHistory> HistoryOfAsync(Guid userId, Guid? categoryId, CancellationToken cancellationToken);
    }
}
