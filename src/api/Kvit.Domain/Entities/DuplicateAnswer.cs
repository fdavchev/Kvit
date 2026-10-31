using Kvit.Domain.Results;

namespace Kvit.Domain.Entities
{
    public class DuplicateAnswer
    {
        private DuplicateAnswer()
        {
        }

        public Guid Id { get; private set; }

        public Guid UserId { get; private set; }

        public Guid PersonalEntryId { get; private set; }

        public Guid ExpenseId { get; private set; }

        public DuplicateAnswerChoice Answer { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public static Result<DuplicateAnswerChoice> CheckAnswer(string? answer)
        {
            if (answer is null || !Enum.GetNames<DuplicateAnswerChoice>().Contains(answer, StringComparer.Ordinal))
            {
                return Result.Failure<DuplicateAnswerChoice>(
                    $"An answer must be the text {nameof(DuplicateAnswerChoice.Same)} or {nameof(DuplicateAnswerChoice.New)}; got {(answer is null ? "no text" : $"'{answer}'")}.",
                    ResultCodes.DUPLICATE_ANSWER_INVALID);
            }

            return Result.Ok(Enum.Parse<DuplicateAnswerChoice>(answer));
        }

        public static Result<T> ExpenseNotCountedFor<T>(Guid expenseId)
        {
            return Result.NotFound<T>(
                $"Expense {expenseId} brings no share to your budget: it does not exist, it or its group was deleted, or your share in it is 0 or not counted.",
                ResultCodes.EXPENSE_NOT_FOUND);
        }

        public static DuplicateAnswer Create(Guid userId, Guid personalEntryId, Guid expenseId, DuplicateAnswerChoice answer, DateTimeOffset createdAt)
        {
            return new DuplicateAnswer
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                PersonalEntryId = personalEntryId,
                ExpenseId = expenseId,
                Answer = answer,
                CreatedAt = createdAt,
            };
        }
    }
}
