using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Commands.Groups
{
    public sealed class UpdateGroupCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        UpdateGroup _updateGroup,
        IUnitOfWork _unitOfWork) : ICommandHandler<UpdateGroupCommand>
    {
        public async Task<Result> Handle(UpdateGroupCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _unitOfWork.RunInTransactionAsync(() => _updateGroup.Execute(command.GroupId, userId.Value, command.Name, command.Emoji, command.Currency, cancellationToken), cancellationToken);
        }
    }
}
