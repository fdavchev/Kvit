using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Commands.Groups
{
    public sealed class StartClosingCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        StartClosing _startClosing,
        GroupWriteTransaction _groupWrite) : ICommandHandler<StartClosingCommand>
    {
        public async Task<Result> Handle(StartClosingCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _groupWrite.RunAsync(command.GroupId, userId.Value, () => _startClosing.Execute(command.GroupId, userId.Value, cancellationToken), cancellationToken);
        }
    }
}
