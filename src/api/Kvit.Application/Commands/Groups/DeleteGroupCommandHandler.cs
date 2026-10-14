using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Commands.Groups
{
    public sealed class DeleteGroupCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        DeleteGroup _deleteGroup,
        GroupWriteTransaction _groupWrite) : ICommandHandler<DeleteGroupCommand>
    {
        public async Task<Result> Handle(DeleteGroupCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _groupWrite.RunAsync(command.GroupId, userId.Value, () => _deleteGroup.Execute(command.GroupId, userId.Value, cancellationToken), cancellationToken);
        }
    }
}
