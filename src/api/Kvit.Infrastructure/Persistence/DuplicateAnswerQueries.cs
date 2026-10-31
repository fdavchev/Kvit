using Kvit.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Infrastructure.Persistence
{
    public static class DuplicateAnswerQueries
    {
        public static IQueryable<DuplicateAnswer> MergesOf(this AppDbContext context, Guid userId)
        {
            IQueryable<CountedShare> countedShares = context.CountedSharesOf(userId);

            return context.DuplicateAnswers
                .AsNoTracking()
                .Where(answer => answer.UserId == userId
                    && answer.Answer == DuplicateAnswerChoice.Same
                    && context.PersonalEntries.Any(entry => entry.Id == answer.PersonalEntryId && entry.DeletedAt == null)
                    && countedShares.Any(row => row.Expense.Id == answer.ExpenseId));
        }
    }
}
