using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.DuplicateAnswers;

namespace Kvit.Application.Commands.DuplicateAnswers
{
    public sealed class RemoveDuplicateAnswerCommandHandler(
        IDispatcher _dispatcher,
        RemoveDuplicateAnswer _removeDuplicateAnswer,
        IUnitOfWork _unitOfWork) : ICommandHandler<RemoveDuplicateAnswerCommand>
    {
        public async Task<Result> Handle(RemoveDuplicateAnswerCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = await _dispatcher.IdOfCallerAsync(cancellationToken);
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _unitOfWork.RunInTransactionAsync(
                () => _removeDuplicateAnswer.Execute(userId.Value, command.EntryId, command.ExpenseId, cancellationToken),
                cancellationToken);
        }
    }
}
