using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.Expenses;
using Kvit.Contracts.Me;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Expenses;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Expenses;

namespace Kvit.Application.Commands.Expenses
{
    public sealed class CreateOneBillCommandHandler(
        IDispatcher _dispatcher,
        CreateOneBill _createOneBill,
        IUnitOfWork _unitOfWork) : ICommandHandler<CreateOneBillCommand, OneBillResponse>
    {
        public async Task<Result<OneBillResponse>> Handle(CreateOneBillCommand command, CancellationToken cancellationToken)
        {
            Result<MeResponse> me = await _dispatcher.FullAccountOfCallerAsync(cancellationToken);
            if (!me.IsSuccess)
            {
                return me.ToFailure<OneBillResponse>();
            }

            Result<OneBillCreated> created = await _unitOfWork.RunInTransactionOnceMoreIfClientRequestIdWasTakenAsync(
                () => _createOneBill.Execute(me.Value.Id, me.Value.DisplayName, command.ClientRequestId, command.Input, cancellationToken),
                cancellationToken);
            if (!created.IsSuccess)
            {
                return created.ToFailure<OneBillResponse>();
            }

            return Result.Ok(new OneBillResponse(created.Value.GroupId, created.Value.ExpenseId));
        }
    }
}
