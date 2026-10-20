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
    public sealed class EditPersonalEntryCommandHandler(
        IDispatcher _dispatcher,
        EditPersonalEntry _editPersonalEntry,
        IUnitOfWork _unitOfWork) : ICommandHandler<EditPersonalEntryCommand, PersonalEntryRow>
    {
        public async Task<Result<PersonalEntryRow>> Handle(EditPersonalEntryCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = await _dispatcher.IdOfCallerAsync(cancellationToken);
            if (!userId.IsSuccess)
            {
                return userId.ToFailure<PersonalEntryRow>();
            }

            Result edited = await _unitOfWork.RunInTransactionAsync(
                () => _editPersonalEntry.Execute(userId.Value, command.EntryId, command.Input, cancellationToken),
                cancellationToken);
            if (!edited.IsSuccess)
            {
                return edited.ToFailure<PersonalEntryRow>();
            }

            return await _dispatcher.Query<GetPersonalEntryQuery, PersonalEntryRow>(new GetPersonalEntryQuery(command.EntryId), cancellationToken);
        }
    }
}
