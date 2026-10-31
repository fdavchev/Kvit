using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Me;
using Kvit.Application.Queries.PersonalEntries;
using Kvit.Contracts.PersonalEntries;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Entities;
using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;
using Kvit.Domain.Services.PersonalEntries;

namespace Kvit.Application.Commands.PersonalEntries
{
    public sealed class EditPersonalEntryCommandHandler(
        IDispatcher _dispatcher,
        EditPersonalEntry _editPersonalEntry,
        EditSpendingEntry _editSpendingEntry,
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
                () => EditAsync(userId.Value, command, cancellationToken),
                cancellationToken);
            if (!edited.IsSuccess)
            {
                return edited.ToFailure<PersonalEntryRow>();
            }

            return await _dispatcher.Query<GetPersonalEntryQuery, PersonalEntryRow>(new GetPersonalEntryQuery(command.EntryId), cancellationToken);
        }

        private Task<Result> EditAsync(Guid userId, EditPersonalEntryCommand command, CancellationToken cancellationToken)
        {
            return command.Kind switch
            {
                PersonalEntryKind.Spending => _editSpendingEntry.Execute(userId, command.EntryId, command.Input, cancellationToken),
                PersonalEntryKind.Income => EditIncomeAsync(userId, command, cancellationToken),
                _ => throw new InvalidOperationException($"Editing a personal entry of kind {command.Kind} is not supported."),
            };
        }

        private async Task<Result> EditIncomeAsync(Guid userId, EditPersonalEntryCommand command, CancellationToken cancellationToken)
        {
            Result<IncomeInput> income = command.Input.ToIncomeInput();
            if (!income.IsSuccess)
            {
                return income;
            }

            return await _editPersonalEntry.Execute(userId, command.EntryId, income.Value, cancellationToken);
        }
    }
}
