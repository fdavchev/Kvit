using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Commands.Groups
{
    public sealed class RemoveGroupPlanCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        RemoveGroupPlan _removeGroupPlan,
        GroupWriteTransaction _groupWrite) : ICommandHandler<RemoveGroupPlanCommand>
    {
        public async Task<Result> Handle(RemoveGroupPlanCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _groupWrite.RunAsync(command.GroupId, userId.Value, () => _removeGroupPlan.Execute(command.GroupId, userId.Value, cancellationToken), cancellationToken);
        }
    }
}
