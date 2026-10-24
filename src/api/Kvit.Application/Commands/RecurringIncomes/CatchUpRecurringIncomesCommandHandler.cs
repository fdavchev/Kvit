using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.Me;
using Kvit.Contracts.Persistence;
using Kvit.Contracts.RecurringIncomes;
using Kvit.Domain.Results;
using Kvit.Domain.Services.RecurringIncomes;

namespace Kvit.Application.Commands.RecurringIncomes
{
    public sealed class CatchUpRecurringIncomesCommandHandler(
        IDispatcher _dispatcher,
        GenerateRecurringIncomeEntries _generateRecurringIncomeEntries,
        IUnitOfWork _unitOfWork) : ICommandHandler<CatchUpRecurringIncomesCommand, RecurringIncomeCatchUpResponse>
    {
        public async Task<Result<RecurringIncomeCatchUpResponse>> Handle(CatchUpRecurringIncomesCommand command, CancellationToken cancellationToken)
        {
            Result<MeResponse> caller = await _dispatcher.FullAccountOfCallerAsync(cancellationToken);
            if (!caller.IsSuccess)
            {
                return caller.ToFailure<RecurringIncomeCatchUpResponse>();
            }

            Result<int> created = await _unitOfWork.RunInTransactionOnceMoreIfRecurringIncomeEntryWasSavedOrDeadlockedAsync(
                () => _generateRecurringIncomeEntries.Execute(caller.Value.Id, caller.Value.TimeZone, cancellationToken),
                cancellationToken);
            if (!created.IsSuccess)
            {
                return created.ToFailure<RecurringIncomeCatchUpResponse>();
            }

            return Result.Ok(new RecurringIncomeCatchUpResponse(created.Value));
        }
    }
}
