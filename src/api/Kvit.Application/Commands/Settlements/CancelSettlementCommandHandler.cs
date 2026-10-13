using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Settlements;

namespace Kvit.Application.Commands.Settlements
{
    public sealed class CancelSettlementCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        CancelSettlement _cancelSettlement,
        IUnitOfWork _unitOfWork) : ICommandHandler<CancelSettlementCommand>
    {
        public async Task<Result> Handle(CancelSettlementCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _unitOfWork.RunInTransactionAsync(() => _cancelSettlement.Execute(command.GroupId, userId.Value, command.SettlementId, cancellationToken), cancellationToken);
        }
    }
}
