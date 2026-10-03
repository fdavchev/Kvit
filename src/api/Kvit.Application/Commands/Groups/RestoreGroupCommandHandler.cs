using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Commands.Groups
{
    public sealed class RestoreGroupCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        RestoreGroup _restoreGroup,
        IUnitOfWork _unitOfWork) : ICommandHandler<RestoreGroupCommand>
    {
        public async Task<Result> Handle(RestoreGroupCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _unitOfWork.RunInTransactionAsync(() => _restoreGroup.Execute(command.GroupId, userId.Value, cancellationToken), cancellationToken);
        }
    }
}
