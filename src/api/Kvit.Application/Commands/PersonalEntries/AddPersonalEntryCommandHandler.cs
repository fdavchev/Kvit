using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Me;
using Kvit.Application.Queries.PersonalEntries;
using Kvit.Contracts.PersonalEntries;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.PersonalEntries;

namespace Kvit.Application.Commands.PersonalEntries
{
    public sealed class AddPersonalEntryCommandHandler(
        IDispatcher _dispatcher,
        AddPersonalEntry _addPersonalEntry,
        IUnitOfWork _unitOfWork) : ICommandHandler<AddPersonalEntryCommand, PersonalEntryRow>
    {
        public async Task<Result<PersonalEntryRow>> Handle(AddPersonalEntryCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = await _dispatcher.IdOfCallerAsync(cancellationToken);
            if (!userId.IsSuccess)
            {
                return userId.ToFailure<PersonalEntryRow>();
            }

            Result<Guid> added = await _unitOfWork.RunInTransactionOnceMoreIfClientRequestIdWasTakenAsync(
                () => _addPersonalEntry.Execute(userId.Value, command.ClientRequestId, command.Input, cancellationToken),
                cancellationToken);
            if (!added.IsSuccess)
            {
                return added.ToFailure<PersonalEntryRow>();
            }

            return await _dispatcher.Query<GetPersonalEntryQuery, PersonalEntryRow>(new GetPersonalEntryQuery(added.Value), cancellationToken);
        }
    }
}
