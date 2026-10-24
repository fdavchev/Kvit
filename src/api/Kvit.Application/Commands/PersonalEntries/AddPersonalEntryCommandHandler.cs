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
                () => AddAsync(userId.Value, command, cancellationToken),
                cancellationToken);
            if (!added.IsSuccess)
            {
                return added.ToFailure<PersonalEntryRow>();
            }

            return await _dispatcher.Query<GetPersonalEntryQuery, PersonalEntryRow>(new GetPersonalEntryQuery(added.Value), cancellationToken);
        }

        private Task<Result<Guid>> AddAsync(Guid userId, AddPersonalEntryCommand command, CancellationToken cancellationToken)
        {
            return command.Kind switch
            {
                PersonalEntryKind.Spending => _addPersonalEntry.Execute(userId, command.ClientRequestId, command.Input, cancellationToken),
                PersonalEntryKind.Income => AddIncomeAsync(userId, command, cancellationToken),
                _ => throw new InvalidOperationException($"Adding a personal entry of kind {command.Kind} is not supported."),
            };
        }

        private async Task<Result<Guid>> AddIncomeAsync(Guid userId, AddPersonalEntryCommand command, CancellationToken cancellationToken)
        {
            Result<IncomeInput> income = command.Input.ToIncomeInput();
            if (!income.IsSuccess)
            {
                return income.ToFailure<Guid>();
            }

            return await _addPersonalEntry.Execute(userId, command.ClientRequestId, income.Value, cancellationToken);
        }
    }
}
