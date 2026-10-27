using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.Me;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.BudgetLimits;

namespace Kvit.Application.Commands.BudgetLimits
{
    public sealed class RemoveBudgetLimitCommandHandler(
        IDispatcher _dispatcher,
        RemoveBudgetLimit _removeBudgetLimit,
        IUnitOfWork _unitOfWork) : ICommandHandler<RemoveBudgetLimitCommand>
    {
        public async Task<Result> Handle(RemoveBudgetLimitCommand command, CancellationToken cancellationToken)
        {
            Result<MeResponse> caller = await _dispatcher.FullAccountOfCallerAsync(cancellationToken);
            if (!caller.IsSuccess)
            {
                return caller;
            }

            return await _unitOfWork.RunInTransactionOnceMoreIfBudgetLimitChangedMeanwhileAsync(
                () => _removeBudgetLimit.Execute(caller.Value.Id, caller.Value.TimeZone, command.CategoryId, cancellationToken),
                cancellationToken);
        }
    }
}
