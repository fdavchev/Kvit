using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.Invites;
using Kvit.Contracts.Me;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Commands.Invites
{
    public sealed class JoinGroupCommandHandler(
        IDispatcher _dispatcher,
        JoinGroup _joinGroup,
        IUnitOfWork _unitOfWork) : ICommandHandler<JoinGroupCommand, JoinGroupResponse>
    {
        public async Task<Result<JoinGroupResponse>> Handle(JoinGroupCommand command, CancellationToken cancellationToken)
        {
            Result<MeResponse> me = await _dispatcher.Query<GetMeQuery, MeResponse>(new GetMeQuery(), cancellationToken);
            if (!me.IsSuccess)
            {
                return me.ToFailure<JoinGroupResponse>();
            }

            Result<Guid> joined = await _unitOfWork.RunInTransactionAsync(
                () => _joinGroup.Execute(command.Token, me.Value.Id, me.Value.DisplayName, command.ClaimMemberId, cancellationToken),
                cancellationToken);
            if (!joined.IsSuccess)
            {
                return joined.ToFailure<JoinGroupResponse>();
            }

            return Result.Ok(new JoinGroupResponse(joined.Value));
        }
    }
}
