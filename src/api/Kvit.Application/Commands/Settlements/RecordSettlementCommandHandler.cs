using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Settlements;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Settlements;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Settlements;

namespace Kvit.Application.Commands.Settlements
{
    public sealed class RecordSettlementCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        IDispatcher _dispatcher,
        RecordSettlement _recordSettlement,
        GroupWriteTransaction _groupWrite) : ICommandHandler<RecordSettlementCommand, SettlementResponse>
    {
        public async Task<Result<SettlementResponse>> Handle(RecordSettlementCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId.ToFailure<SettlementResponse>();
            }

            Result<Guid> recorded = await _groupWrite.RunOnceMoreIfClientRequestIdWasTakenAsync(
                command.GroupId,
                userId.Value,
                () => _recordSettlement.Execute(command.GroupId, userId.Value, command.ClientRequestId, command.Input, cancellationToken),
                cancellationToken);
            if (!recorded.IsSuccess)
            {
                return recorded.ToFailure<SettlementResponse>();
            }

            return await _dispatcher.Query<GetSettlementQuery, SettlementResponse>(new GetSettlementQuery(command.GroupId, recorded.Value), cancellationToken);
        }
    }
}
