using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.PersonalEntries;

namespace Kvit.Application.Commands.PersonalEntries
{
    public sealed class DeletePersonalEntryCommandHandler(
        IDispatcher _dispatcher,
        DeletePersonalEntry _deletePersonalEntry,
        IUnitOfWork _unitOfWork) : ICommandHandler<DeletePersonalEntryCommand>
    {
        public async Task<Result> Handle(DeletePersonalEntryCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = await _dispatcher.IdOfCallerAsync(cancellationToken);
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _unitOfWork.RunInTransactionAsync(() => _deletePersonalEntry.Execute(userId.Value, command.EntryId, cancellationToken), cancellationToken);
        }
    }
}
