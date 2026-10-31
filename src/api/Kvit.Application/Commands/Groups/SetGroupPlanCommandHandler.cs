using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Commands.Groups
{
    public sealed class SetGroupPlanCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        SetGroupPlan _setGroupPlan,
        GroupWriteTransaction _groupWrite) : ICommandHandler<SetGroupPlanCommand>
    {
        public async Task<Result> Handle(SetGroupPlanCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _groupWrite.RunAsync(command.GroupId, userId.Value, () => _setGroupPlan.Execute(command.GroupId, userId.Value, command.AmountMinor, command.Currency, cancellationToken), cancellationToken);
        }
    }
}
