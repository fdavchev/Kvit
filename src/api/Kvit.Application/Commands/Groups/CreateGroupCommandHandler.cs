using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Groups;
using Kvit.Application.Queries.Me;
using Kvit.Contracts.Groups;
using Kvit.Contracts.Me;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Commands.Groups
{
    public sealed class CreateGroupCommandHandler(
        IDispatcher _dispatcher,
        CreateGroup _createGroup,
        IUnitOfWork _unitOfWork) : ICommandHandler<CreateGroupCommand, GroupResponse>
    {
        public async Task<Result<GroupResponse>> Handle(CreateGroupCommand command, CancellationToken cancellationToken)
        {
            Result<MeResponse> me = await _dispatcher.Query<GetMeQuery, MeResponse>(new GetMeQuery(), cancellationToken);
            if (!me.IsSuccess)
            {
                return Result.Unauthorized<GroupResponse>(me.Error, me.ErrorCode);
            }

            Result<Guid> created = await _unitOfWork.RunInTransactionAsync(() => Task.FromResult(_createGroup.Execute(me.Value.Id, me.Value.DisplayName, command.Name, command.Emoji, command.Currency)), cancellationToken);
            if (!created.IsSuccess)
            {
                return Result.Failure<GroupResponse>(created.Error, created.ErrorCode);
            }

            return await _dispatcher.Query<GetGroupQuery, GroupResponse>(new GetGroupQuery(created.Value), cancellationToken);
        }
    }
}
