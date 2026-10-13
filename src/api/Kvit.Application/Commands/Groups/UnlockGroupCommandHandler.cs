using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Commands.Groups
{
    public sealed class UnlockGroupCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        UnlockGroup _unlockGroup,
        IUnitOfWork _unitOfWork) : ICommandHandler<UnlockGroupCommand>
    {
        public async Task<Result> Handle(UnlockGroupCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _unitOfWork.RunInTransactionAsync(() => _unlockGroup.Execute(command.GroupId, userId.Value, cancellationToken), cancellationToken);
        }
    }
}
