using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.DuplicateAnswers
{
    public sealed class RemoveDuplicateAnswer(IDuplicateAnswerRepository _answers)
    {
        public async Task<Result> Execute(Guid userId, Guid entryId, Guid expenseId, CancellationToken cancellationToken)
        {
            await _answers.RemoveAsync(userId, entryId, expenseId, cancellationToken);

            return Result.Ok();
        }
    }
}
