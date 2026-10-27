using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.DuplicateAnswers;

namespace Kvit.Application.Commands.DuplicateAnswers
{
    public sealed class SetDuplicateAnswerCommandHandler(
        IDispatcher _dispatcher,
        SetDuplicateAnswer _setDuplicateAnswer,
        IUnitOfWork _unitOfWork) : ICommandHandler<SetDuplicateAnswerCommand>
    {
        public async Task<Result> Handle(SetDuplicateAnswerCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = await _dispatcher.IdOfCallerAsync(cancellationToken);
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _unitOfWork.RunInTransactionAsync(
                () => _setDuplicateAnswer.Execute(userId.Value, command.EntryId, command.ExpenseId, command.Answer, cancellationToken),
                cancellationToken);
        }
    }
}
