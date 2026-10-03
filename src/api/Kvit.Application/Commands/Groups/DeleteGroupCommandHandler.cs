using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Commands.Groups
{
    public sealed class DeleteGroupCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        DeleteGroup _deleteGroup,
        IUnitOfWork _unitOfWork) : ICommandHandler<DeleteGroupCommand>
    {
        public async Task<Result> Handle(DeleteGroupCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            await _unitOfWork.OpenTransactionAsync(cancellationToken);
            Result result = await _deleteGroup.Execute(command.GroupId, userId.Value, cancellationToken);
            if (!result.IsSuccess)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                return result;
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return result;
        }
    }
}
