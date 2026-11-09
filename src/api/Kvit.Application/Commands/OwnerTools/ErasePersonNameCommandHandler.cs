using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Contracts.OwnerTools;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;

namespace Kvit.Application.Commands.OwnerTools
{
    public sealed class ErasePersonNameCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        IAccountModeration _accountModeration) : ICommandHandler<ErasePersonNameCommand>
    {
        public async Task<Result> Handle(ErasePersonNameCommand command, CancellationToken cancellationToken)
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

            return await _accountModeration.EraseNameAsync(adminUserId.Value, command.PersonId, reason.Value, cancellationToken);
        }
    }
}
