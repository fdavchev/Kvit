using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Commands.Groups
{
    public sealed class MakeOwnerCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        MakeOwner _makeOwner,
        GroupWriteTransaction _groupWrite) : ICommandHandler<MakeOwnerCommand>
    {
        public async Task<Result> Handle(MakeOwnerCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _groupWrite.RunAsync(command.GroupId, userId.Value, () => _makeOwner.Execute(command.GroupId, userId.Value, command.MemberId, cancellationToken), cancellationToken);
        }
    }
}
