using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Me;
using Kvit.Application.Queries.RecurringIncomes;
using Kvit.Contracts.Persistence;
using Kvit.Contracts.RecurringIncomes;
using Kvit.Domain.Results;
using Kvit.Domain.Services.RecurringIncomes;

namespace Kvit.Application.Commands.RecurringIncomes
{
    public sealed class EditRecurringIncomeCommandHandler(
        IDispatcher _dispatcher,
        EditRecurringIncome _editRecurringIncome,
        IUnitOfWork _unitOfWork) : ICommandHandler<EditRecurringIncomeCommand, RecurringIncomeRow>
    {
        public async Task<Result<RecurringIncomeRow>> Handle(EditRecurringIncomeCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = await _dispatcher.IdOfCallerAsync(cancellationToken);
            if (!userId.IsSuccess)
            {
                return userId.ToFailure<RecurringIncomeRow>();
            }

            Result edited = await _unitOfWork.RunInTransactionAsync(
                () => _editRecurringIncome.Execute(userId.Value, command.RecurringIncomeId, command.Input, cancellationToken),
                cancellationToken);
            if (!edited.IsSuccess)
            {
                return edited.ToFailure<RecurringIncomeRow>();
            }

            return await _dispatcher.Query<GetRecurringIncomeQuery, RecurringIncomeRow>(new GetRecurringIncomeQuery(command.RecurringIncomeId), cancellationToken);
        }
    }
}
