using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.RecurringIncomes;

namespace Kvit.Application.Commands.RecurringIncomes
{
    public sealed class StopRecurringIncomeCommandHandler(
        IDispatcher _dispatcher,
        StopRecurringIncome _stopRecurringIncome,
        IUnitOfWork _unitOfWork) : ICommandHandler<StopRecurringIncomeCommand>
    {
        public async Task<Result> Handle(StopRecurringIncomeCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = await _dispatcher.IdOfCallerAsync(cancellationToken);
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _unitOfWork.RunInTransactionAsync(() => _stopRecurringIncome.Execute(userId.Value, command.RecurringIncomeId, cancellationToken), cancellationToken);
        }
    }
}
