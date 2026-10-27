using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Infrastructure.Repositories
{
    public sealed class DuplicateAnswerRepository(AppDbContext _context) : IDuplicateAnswerRepository
    {
        public async Task SaveAsync(DuplicateAnswer answer, CancellationToken cancellationToken)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO duplicate_answers (id, user_id, personal_entry_id, expense_id, answer, created_at)
                VALUES ({answer.Id}, {answer.UserId}, {answer.PersonalEntryId}, {answer.ExpenseId}, {answer.Answer.ToString()}, {answer.CreatedAt})
                ON CONFLICT (user_id, personal_entry_id, expense_id) DO UPDATE SET answer = EXCLUDED.answer
                WHERE duplicate_answers.answer <> EXCLUDED.answer
                """,
                cancellationToken);
        }

        public async Task RemoveAsync(Guid userId, Guid personalEntryId, Guid expenseId, CancellationToken cancellationToken)
        {
            await _context.DuplicateAnswers
                .Where(answer => answer.UserId == userId && answer.PersonalEntryId == personalEntryId && answer.ExpenseId == expenseId)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
