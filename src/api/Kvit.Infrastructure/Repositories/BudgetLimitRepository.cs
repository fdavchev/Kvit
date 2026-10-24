using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Infrastructure.Repositories
{
    public sealed class BudgetLimitRepository(AppDbContext _context) : IBudgetLimitRepository
    {
        public void Add(BudgetLimit limit)
        {
            _context.BudgetLimits.Add(limit);
        }

        public void Remove(BudgetLimit limit)
        {
            _context.BudgetLimits.Remove(limit);
        }

        public async Task<BudgetLimitHistory> HistoryOfAsync(Guid userId, Guid? categoryId, CancellationToken cancellationToken)
        {
            List<BudgetLimit> rows = await _context.BudgetLimits
                .Where(limit => limit.UserId == userId && limit.CategoryId == categoryId)
                .ToListAsync(cancellationToken);

            return new BudgetLimitHistory(rows);
        }
    }
}
