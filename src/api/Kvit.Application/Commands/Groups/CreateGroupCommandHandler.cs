using Kvit.Application.Dispatching;
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

            await _unitOfWork.OpenTransactionAsync(cancellationToken);
            Result<Guid> created = _createGroup.Execute(me.Value.Id, me.Value.DisplayName, command.Name, command.Emoji, command.Currency);
            if (!created.IsSuccess)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure<GroupResponse>(created.Error, created.ErrorCode);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return await _dispatcher.Query<GetGroupQuery, GroupResponse>(new GetGroupQuery(created.Value), cancellationToken);
        }
    }
}
