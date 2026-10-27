using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.DuplicateAnswers
{
    public sealed class SetDuplicateAnswer(
        IPersonalEntryRepository _entries,
        IExpenseRepository _expenses,
        IDuplicateAnswerRepository _answers,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid userId, Guid entryId, Guid expenseId, string? answer, CancellationToken cancellationToken)
        {
            Result<PersonalEntry> entry = await _entries.FindOwnedAsync(userId, entryId, cancellationToken);
            if (!entry.IsSuccess)
            {
                return entry;
            }

            if (entry.Value.Kind != PersonalEntryKind.Spending)
            {
                return PersonalEntry.NotFound<PersonalEntry>(entryId);
            }

            if (!await _expenses.BringsCountedShareToAsync(userId, expenseId, cancellationToken))
            {
                return DuplicateAnswer.ExpenseNotCountedFor<DuplicateAnswer>(expenseId);
            }

            Result<DuplicateAnswerChoice> choice = DuplicateAnswer.CheckAnswer(answer);
            if (!choice.IsSuccess)
            {
                return choice;
            }

            await _answers.SaveAsync(DuplicateAnswer.Create(userId, entryId, expenseId, choice.Value, _timeProvider.GetUtcNow()), cancellationToken);

            return Result.Ok();
        }
    }
}
