using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Settlements;

namespace Kvit.Application.Commands.Settlements
{
    public sealed class RestoreSettlementCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        RestoreSettlement _restoreSettlement,
        IUnitOfWork _unitOfWork) : ICommandHandler<RestoreSettlementCommand>
    {
        public async Task<Result> Handle(RestoreSettlementCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _unitOfWork.RunInTransactionAsync(() => _restoreSettlement.Execute(command.GroupId, userId.Value, command.SettlementId, cancellationToken), cancellationToken);
        }
    }
}
