using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Infrastructure.Repositories
{
    public sealed class ExpenseRepository(AppDbContext _context) : IExpenseRepository
    {
        public void Add(Expense expense)
        {
            _context.Expenses.Add(expense);
        }

        public Task<Expense?> FindByClientRequestIdAsync(Guid clientRequestId, CancellationToken cancellationToken)
        {
            return _context.Expenses
                .AsNoTracking()
                .SingleOrDefaultAsync(expense => expense.ClientRequestId == clientRequestId, cancellationToken);
        }

        public Task<Result<Expense>> FindInGroupAsync(Guid groupId, Guid expenseId, CancellationToken cancellationToken)
        {
            return SingleOrNotFoundAsync(_context.Expenses.Where(expense => expense.DeletedAt == null), groupId, expenseId, cancellationToken);
        }

        public Task<Result<Expense>> FindIncludingDeletedInGroupAsync(Guid groupId, Guid expenseId, CancellationToken cancellationToken)
        {
            return SingleOrNotFoundAsync(_context.Expenses, groupId, expenseId, cancellationToken);
        }

        public Task<Currency?> FindCurrencyAsync(Guid groupId, Guid expenseId, CancellationToken cancellationToken)
        {
            return _context.Expenses
                .AsNoTracking()
                .Where(expense => expense.Id == expenseId && expense.GroupId == groupId && expense.DeletedAt == null)
                .Select(expense => (Currency?)expense.Currency)
                .SingleOrDefaultAsync(cancellationToken);
        }

        private static async Task<Result<Expense>> SingleOrNotFoundAsync(IQueryable<Expense> expenses, Guid groupId, Guid expenseId, CancellationToken cancellationToken)
        {
            Expense? expense = await expenses
                .Include(candidate => candidate.Shares)
                .SingleOrDefaultAsync(candidate => candidate.Id == expenseId && candidate.GroupId == groupId, cancellationToken);
            if (expense is null)
            {
                return Expense.NotFound<Expense>(expenseId);
            }

            return Result.Ok(expense);
        }
    }
}
