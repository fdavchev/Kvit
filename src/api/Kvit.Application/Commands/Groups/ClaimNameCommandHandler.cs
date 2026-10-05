using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Commands.Groups
{
    public sealed class ClaimNameCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        ClaimName _claimName,
        IUnitOfWork _unitOfWork) : ICommandHandler<ClaimNameCommand>
    {
        public async Task<Result> Handle(ClaimNameCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _unitOfWork.RunInTransactionAsync(() => _claimName.Execute(command.GroupId, userId.Value, command.MemberId, cancellationToken), cancellationToken);
        }
    }
}
