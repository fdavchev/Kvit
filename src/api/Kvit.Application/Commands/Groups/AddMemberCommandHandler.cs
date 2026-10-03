using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Groups;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Groups;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Commands.Groups
{
    public sealed class AddMemberCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        IDispatcher _dispatcher,
        AddMember _addMember,
        IUnitOfWork _unitOfWork) : ICommandHandler<AddMemberCommand, GroupMemberRow>
    {
        public async Task<Result<GroupMemberRow>> Handle(AddMemberCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId.ToFailure<GroupMemberRow>();
            }

            Result<Guid> added = await _unitOfWork.RunInTransactionAsync(() => _addMember.Execute(command.GroupId, userId.Value, command.Name, cancellationToken), cancellationToken);
            if (!added.IsSuccess)
            {
                return added.ToFailure<GroupMemberRow>();
            }

            Result<GroupMembersResponse> members = await _dispatcher.Query<GetGroupMembersQuery, GroupMembersResponse>(new GetGroupMembersQuery(command.GroupId), cancellationToken);
            if (!members.IsSuccess)
            {
                return members.ToFailure<GroupMemberRow>();
            }

            GroupMemberRow row = members.Value.Members.SingleOrDefault(member => member.Id == added.Value)
                ?? throw new InvalidOperationException($"Member {added.Value} was added to group {command.GroupId} but is missing from its members list.");

            return Result.Ok(row);
        }
    }
}
