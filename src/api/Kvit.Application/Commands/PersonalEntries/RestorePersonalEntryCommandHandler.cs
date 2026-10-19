using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.PersonalEntries;

namespace Kvit.Application.Commands.PersonalEntries
{
    public sealed class RestorePersonalEntryCommandHandler(
        IDispatcher _dispatcher,
        RestorePersonalEntry _restorePersonalEntry,
        IUnitOfWork _unitOfWork) : ICommandHandler<RestorePersonalEntryCommand>
    {
        public async Task<Result> Handle(RestorePersonalEntryCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = await _dispatcher.IdOfCallerAsync(cancellationToken);
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _unitOfWork.RunInTransactionAsync(() => _restorePersonalEntry.Execute(userId.Value, command.EntryId, cancellationToken), cancellationToken);
        }
    }
}
