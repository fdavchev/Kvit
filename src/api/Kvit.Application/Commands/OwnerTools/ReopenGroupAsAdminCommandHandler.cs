using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Services.AdminActions;

namespace Kvit.Application.Commands.OwnerTools
{
    public sealed class ReopenGroupAsAdminCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        ReopenGroupAsAdmin _reopenGroupAsAdmin,
        IUnitOfWork _unitOfWork) : ICommandHandler<ReopenGroupAsAdminCommand>
    {
        public async Task<Result> Handle(ReopenGroupAsAdminCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> adminUserId = _currentUserProvider.GetCurrentUserId();
            if (!adminUserId.IsSuccess)
            {
                return adminUserId;
            }

            Result<string> reason = AdminAction.ReasonOf(command.Reason);
            if (!reason.IsSuccess)
            {
                return reason;
            }

            return await _unitOfWork.RunInTransactionAsync(() => _reopenGroupAsAdmin.Execute(adminUserId.Value, command.GroupId, reason.Value, cancellationToken), cancellationToken);
        }
    }
}
