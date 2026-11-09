using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Services.AdminActions;

namespace Kvit.Application.Commands.OwnerTools
{
    public sealed class CloseGroupAsAdminCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        CloseGroupAsAdmin _closeGroupAsAdmin,
        IUnitOfWork _unitOfWork) : ICommandHandler<CloseGroupAsAdminCommand>
    {
        public async Task<Result> Handle(CloseGroupAsAdminCommand command, CancellationToken cancellationToken)
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

            return await _unitOfWork.RunInTransactionAsync(() => _closeGroupAsAdmin.Execute(adminUserId.Value, command.GroupId, reason.Value, cancellationToken), cancellationToken);
        }
    }
}
