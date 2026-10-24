using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.Me;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.BudgetLimits;

namespace Kvit.Application.Commands.BudgetLimits
{
    public sealed class SetBudgetLimitCommandHandler(
        IDispatcher _dispatcher,
        SetBudgetLimit _setBudgetLimit,
        IUnitOfWork _unitOfWork) : ICommandHandler<SetBudgetLimitCommand>
    {
        public async Task<Result> Handle(SetBudgetLimitCommand command, CancellationToken cancellationToken)
        {
            Result<MeResponse> caller = await _dispatcher.FullAccountOfCallerAsync(cancellationToken);
            if (!caller.IsSuccess)
            {
                return caller;
            }

            return await _unitOfWork.RunInTransactionOnceMoreIfBudgetLimitChangedMeanwhileAsync(
                () => _setBudgetLimit.Execute(caller.Value.Id, caller.Value.TimeZone, command.CategoryId, command.LimitMinor, cancellationToken),
                cancellationToken);
        }
    }
}
