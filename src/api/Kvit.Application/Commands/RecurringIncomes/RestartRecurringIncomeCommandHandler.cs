using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.RecurringIncomes;

namespace Kvit.Application.Commands.RecurringIncomes
{
    public sealed class RestartRecurringIncomeCommandHandler(
        IDispatcher _dispatcher,
        RestartRecurringIncome _restartRecurringIncome,
        IUnitOfWork _unitOfWork) : ICommandHandler<RestartRecurringIncomeCommand>
    {
        public async Task<Result> Handle(RestartRecurringIncomeCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = await _dispatcher.IdOfCallerAsync(cancellationToken);
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _unitOfWork.RunInTransactionAsync(() => _restartRecurringIncome.Execute(userId.Value, command.RecurringIncomeId, cancellationToken), cancellationToken);
        }
    }
}
