using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Infrastructure.Repositories
{
    public sealed class RecurringIncomeRepository(AppDbContext _context) : IRecurringIncomeRepository
    {
        public void Add(RecurringIncome income)
        {
            _context.RecurringIncomes.Add(income);
        }

        public Task<RecurringIncome?> FindByClientRequestIdAsync(Guid clientRequestId, CancellationToken cancellationToken)
        {
            return _context.RecurringIncomes
                .AsNoTracking()
                .SingleOrDefaultAsync(income => income.ClientRequestId == clientRequestId, cancellationToken);
        }

        public Task<Result<RecurringIncome>> FindActiveOwnedAsync(Guid userId, Guid recurringIncomeId, CancellationToken cancellationToken)
        {
            return SingleOrNotFoundAsync(_context.RecurringIncomes.Where(income => income.StoppedAt == null), userId, recurringIncomeId, cancellationToken);
        }

        public Task<Result<RecurringIncome>> FindOwnedIncludingStoppedAsync(Guid userId, Guid recurringIncomeId, CancellationToken cancellationToken)
        {
            return SingleOrNotFoundAsync(_context.RecurringIncomes, userId, recurringIncomeId, cancellationToken);
        }

        public async Task<int> LockAndCountActiveOfAsync(Guid userId, CancellationToken cancellationToken)
        {
            if (_context.Database.CurrentTransaction is null)
            {
                throw new InvalidOperationException($"Counting the active repeating incomes of user {userId} needs an open transaction, because the lock it takes lasts until the transaction ends.");
            }

            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext('recurring_incomes'), hashtext({userId.ToString()}))",
                cancellationToken);

            return await _context.RecurringIncomes.CountAsync(income => income.UserId == userId && income.StoppedAt == null, cancellationToken);
        }

        public async Task<IReadOnlyList<RecurringIncome>> ListActiveOfInCreationOrderAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _context.RecurringIncomes
                .AsNoTracking()
                .Where(income => income.UserId == userId && income.StoppedAt == null)
                .OrderBy(income => income.CreatedAt)
                .ThenBy(income => income.Id)
                .ToListAsync(cancellationToken);
        }

        private static async Task<Result<RecurringIncome>> SingleOrNotFoundAsync(IQueryable<RecurringIncome> incomes, Guid userId, Guid recurringIncomeId, CancellationToken cancellationToken)
        {
            RecurringIncome? income = await incomes.SingleOrDefaultAsync(candidate => candidate.Id == recurringIncomeId && candidate.UserId == userId, cancellationToken);
            if (income is null)
            {
                return RecurringIncome.NotFound<RecurringIncome>(recurringIncomeId);
            }

            return Result.Ok(income);
        }
    }
}
