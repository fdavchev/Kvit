using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Application.Queries.Me;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Application.Commands.Groups
{
    public sealed class SetCountsInBudgetCommandHandler(
        IDispatcher _dispatcher,
        SetCountsInBudget _setCountsInBudget,
        GroupWriteTransaction _groupWrite) : ICommandHandler<SetCountsInBudgetCommand>
    {
        public async Task<Result> Handle(SetCountsInBudgetCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = await _dispatcher.IdOfCallerAsync(cancellationToken);
            if (!userId.IsSuccess)
            {
                return userId;
            }

            return await _groupWrite.RunOnceMoreIfMemberChangedMeanwhileAsync(command.GroupId, userId.Value, () => _setCountsInBudget.Execute(command.GroupId, userId.Value, command.CountsInBudget, cancellationToken), cancellationToken);
        }
    }
}
