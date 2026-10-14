using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Commands.Groups
{
    public sealed class ObjectToClosingCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        ObjectToClosing _objectToClosing,
        GroupWriteTransaction _groupWrite) : ICommandHandler<ObjectToClosingCommand>
    {
        public async Task<Result> Handle(ObjectToClosingCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _groupWrite.RunAsync(command.GroupId, userId.Value, () => _objectToClosing.Execute(command.GroupId, userId.Value, command.Reason, cancellationToken), cancellationToken);
        }
    }
}
