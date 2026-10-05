using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Commands.Groups
{
    public sealed class UndoClaimCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        UndoClaim _undoClaim,
        IUnitOfWork _unitOfWork) : ICommandHandler<UndoClaimCommand>
    {
        public async Task<Result> Handle(UndoClaimCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _unitOfWork.RunInTransactionAsync(() => _undoClaim.Execute(command.GroupId, userId.Value, command.MemberId, cancellationToken), cancellationToken);
        }
    }
}
