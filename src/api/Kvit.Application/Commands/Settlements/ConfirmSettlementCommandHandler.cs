using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Settlements;

namespace Kvit.Application.Commands.Settlements
{
    public sealed class ConfirmSettlementCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        ConfirmSettlement _confirmSettlement,
        GroupWriteTransaction _groupWrite) : ICommandHandler<ConfirmSettlementCommand>
    {
        public async Task<Result> Handle(ConfirmSettlementCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _groupWrite.RunAsync(command.GroupId, userId.Value, () => _confirmSettlement.Execute(command.GroupId, userId.Value, command.SettlementId, cancellationToken), cancellationToken);
        }
    }
}
