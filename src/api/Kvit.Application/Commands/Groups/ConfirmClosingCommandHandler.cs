using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Commands.Groups
{
    public sealed class ConfirmClosingCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        ConfirmClosing _confirmClosing,
        GroupWriteTransaction _groupWrite) : ICommandHandler<ConfirmClosingCommand>
    {
        public async Task<Result> Handle(ConfirmClosingCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _groupWrite.RunAsync(command.GroupId, userId.Value, () => _confirmClosing.Execute(command.GroupId, userId.Value, cancellationToken), cancellationToken);
        }
    }
}
