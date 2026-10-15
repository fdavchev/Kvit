using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Groups;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Commands.Groups
{
    public sealed class ResetInviteLinkCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        ResetInviteLink _resetInviteLink,
        GroupWriteTransaction _groupWrite) : ICommandHandler<ResetInviteLinkCommand, InviteTokenResponse>
    {
        public async Task<Result<InviteTokenResponse>> Handle(ResetInviteLinkCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId.ToFailure<InviteTokenResponse>();
            }

            Result<string> inviteToken = await _groupWrite.RunAsync(command.GroupId, userId.Value, () => _resetInviteLink.Execute(command.GroupId, userId.Value, cancellationToken), cancellationToken);
            if (!inviteToken.IsSuccess)
            {
                return inviteToken.ToFailure<InviteTokenResponse>();
            }

            return Result.Ok(new InviteTokenResponse(inviteToken.Value));
        }
    }
}
