using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Commands.Groups
{
    public sealed class LetGuestInAgainCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        LetGuestInAgain _letGuestInAgain,
        GroupWriteTransaction _groupWrite) : ICommandHandler<LetGuestInAgainCommand>
    {
        public async Task<Result> Handle(LetGuestInAgainCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _groupWrite.RunAsync(command.GroupId, userId.Value, () => _letGuestInAgain.Execute(command.GroupId, userId.Value, command.MemberId, cancellationToken), cancellationToken);
        }
    }
}
