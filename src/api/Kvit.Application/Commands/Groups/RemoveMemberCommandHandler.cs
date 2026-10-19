using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Commands.Groups
{
    public sealed class RemoveMemberCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        RemoveMember _removeMember,
        GroupWriteTransaction _groupWrite) : ICommandHandler<RemoveMemberCommand>
    {
        public async Task<Result> Handle(RemoveMemberCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _groupWrite.RunOnceMoreIfMemberChangedMeanwhileAsync(command.GroupId, userId.Value, () => _removeMember.Execute(command.GroupId, userId.Value, command.MemberId, cancellationToken), cancellationToken);
        }
    }
}
