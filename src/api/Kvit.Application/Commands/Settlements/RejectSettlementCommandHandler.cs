using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Settlements;

namespace Kvit.Application.Commands.Settlements
{
    public sealed class RejectSettlementCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        RejectSettlement _rejectSettlement,
        GroupWriteTransaction _groupWrite) : ICommandHandler<RejectSettlementCommand>
    {
        public async Task<Result> Handle(RejectSettlementCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _groupWrite.RunAsync(command.GroupId, userId.Value, () => _rejectSettlement.Execute(command.GroupId, userId.Value, command.SettlementId, cancellationToken), cancellationToken);
        }
    }
}
