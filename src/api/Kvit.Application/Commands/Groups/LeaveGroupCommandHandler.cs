using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Commands.Groups
{
    public sealed class LeaveGroupCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        LeaveGroup _leaveGroup,
        GroupWriteTransaction _groupWrite) : ICommandHandler<LeaveGroupCommand>
    {
        public async Task<Result> Handle(LeaveGroupCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _groupWrite.RunAsync(command.GroupId, userId.Value, () => _leaveGroup.Execute(command.GroupId, userId.Value, cancellationToken), cancellationToken);
        }
    }
}
