using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Commands.Groups
{
    public sealed class ClaimNameCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        ClaimName _claimName,
        GroupWriteTransaction _groupWrite) : ICommandHandler<ClaimNameCommand>
    {
        public async Task<Result> Handle(ClaimNameCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _groupWrite.RunAsync(command.GroupId, userId.Value, () => _claimName.Execute(command.GroupId, userId.Value, command.MemberId, cancellationToken), cancellationToken);
        }
    }
}
