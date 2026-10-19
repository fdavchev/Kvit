using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Invites;
using Kvit.Contracts.Me;
using Kvit.Domain.Accounts;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Commands.Invites
{
    public sealed class JoinAsGuestCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        IAccountService _accountService,
        PickGuestName _pickGuestName,
        JoinGroup _joinGroup,
        GroupWriteTransaction _groupWrite) : ICommandHandler<JoinAsGuestCommand, JoinGroupResponse>
    {
        public async Task<Result<JoinGroupResponse>> Handle(JoinAsGuestCommand command, CancellationToken cancellationToken)
        {
            if (_currentUserProvider.GetCurrentUserId().IsSuccess)
            {
                return Result.Conflict<JoinGroupResponse>("Someone is already signed in on this browser, so no guest is made. Join with that account instead.", ResultCodes.GUEST_JOIN_SIGNED_IN);
            }

            Result<GuestJoinChoice> choice = GuestJoinChoice.Create(command.ClaimMemberId, command.Name, command.TimeZone, command.Language);
            if (!choice.IsSuccess)
            {
                return choice.ToFailure<JoinGroupResponse>();
            }

            Result<GuestJoinedDto> joined = await _groupWrite.RunForInviteOnceMoreIfMemberChangedMeanwhileAsync(
                command.Token,
                () => CreateGuestAndJoinAsync(command.Token, choice.Value, cancellationToken),
                cancellationToken);
            if (!joined.IsSuccess)
            {
                return joined.ToFailure<JoinGroupResponse>();
            }

            await _accountService.SignInAsync(joined.Value.GuestUserId);

            return Result.Ok(new JoinGroupResponse(joined.Value.GroupId));
        }

        private async Task<Result<GuestJoinedDto>> CreateGuestAndJoinAsync(string inviteToken, GuestJoinChoice choice, CancellationToken cancellationToken)
        {
            Result<string> name = await _pickGuestName.Execute(inviteToken, choice, cancellationToken);
            if (!name.IsSuccess)
            {
                return name.ToFailure<GuestJoinedDto>();
            }

            Result<MeResponse> guest = await _accountService.CreateGuestAsync(name.Value, choice.Language, choice.TimeZone, cancellationToken);
            if (!guest.IsSuccess)
            {
                return guest.ToFailure<GuestJoinedDto>();
            }

            Result<Guid> joined = await _joinGroup.ExecuteForGuest(inviteToken, guest.Value.Id, guest.Value.DisplayName, choice.ClaimMemberId, cancellationToken);
            if (!joined.IsSuccess)
            {
                return joined.ToFailure<GuestJoinedDto>();
            }

            return Result.Ok(new GuestJoinedDto(guest.Value.Id, joined.Value));
        }
    }
}
