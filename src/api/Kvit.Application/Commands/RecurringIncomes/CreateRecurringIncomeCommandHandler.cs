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
    public sealed class CreateRecurringIncomeCommandHandler(
        IDispatcher _dispatcher,
        CreateRecurringIncome _createRecurringIncome,
        IUnitOfWork _unitOfWork) : ICommandHandler<CreateRecurringIncomeCommand, RecurringIncomeRow>
    {
        public async Task<Result<RecurringIncomeRow>> Handle(CreateRecurringIncomeCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = await _dispatcher.IdOfCallerAsync(cancellationToken);
            if (!userId.IsSuccess)
            {
                return userId.ToFailure<RecurringIncomeRow>();
            }

            Result<Guid> created = await _unitOfWork.RunInTransactionOnceMoreIfClientRequestIdWasTakenAsync(
                () => _createRecurringIncome.Execute(userId.Value, command.ClientRequestId, command.Input, cancellationToken),
                cancellationToken);
            if (!created.IsSuccess)
            {
                return created.ToFailure<RecurringIncomeRow>();
            }

            return await _dispatcher.Query<GetRecurringIncomeQuery, RecurringIncomeRow>(new GetRecurringIncomeQuery(created.Value), cancellationToken);
        }
    }
}
