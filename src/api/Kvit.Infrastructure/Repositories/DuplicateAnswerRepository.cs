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
            await _context.ExecuteSqlOrReportMergeAlreadySavedAsync(
                $"""
                INSERT INTO duplicate_answers (id, user_id, personal_entry_id, expense_id, answer, created_at)
                VALUES ({answer.Id}, {answer.UserId}, {answer.PersonalEntryId}, {answer.ExpenseId}, {answer.Answer.ToString()}, {answer.CreatedAt})
                ON CONFLICT (user_id, personal_entry_id, expense_id) DO UPDATE SET answer = EXCLUDED.answer
                WHERE duplicate_answers.answer <> EXCLUDED.answer
                """,
                cancellationToken);
        }

        public async Task RemoveSameAnswersOfEntryAsync(Guid userId, Guid personalEntryId, CancellationToken cancellationToken)
        {
            await _context.DuplicateAnswers
                .Where(answer => answer.UserId == userId && answer.PersonalEntryId == personalEntryId && answer.Answer == DuplicateAnswerChoice.Same)
                .ExecuteDeleteAsync(cancellationToken);
        }

        public async Task RemoveEndedMergesOfEitherAsync(Guid userId, Guid personalEntryId, Guid expenseId, CancellationToken cancellationToken)
        {
            IQueryable<DuplicateAnswer> merges = _context.MergesOf(userId);

            await _context.DuplicateAnswers
                .Where(answer => answer.UserId == userId
                    && answer.Answer == DuplicateAnswerChoice.Same
                    && ((answer.PersonalEntryId == personalEntryId && answer.ExpenseId != expenseId)
                        || (answer.ExpenseId == expenseId && answer.PersonalEntryId != personalEntryId))
                    && !merges.Any(merge => merge.Id == answer.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        public Task<bool> IsEitherMergedWithAnotherAsync(Guid userId, Guid personalEntryId, Guid expenseId, CancellationToken cancellationToken)
        {
            return _context.MergesOf(userId).AnyAsync(
                merge => (merge.PersonalEntryId == personalEntryId && merge.ExpenseId != expenseId)
                    || (merge.ExpenseId == expenseId && merge.PersonalEntryId != personalEntryId),
                cancellationToken);
        }
    }
}
