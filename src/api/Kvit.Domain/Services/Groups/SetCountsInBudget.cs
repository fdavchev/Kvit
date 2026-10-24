using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class SetCountsInBudget(
        IGroupRepository _groups,
        IGroupMemberRepository _members,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid groupId, Guid userId, bool countsInBudget, CancellationToken cancellationToken)
        {
            Result<Group> found = await _groups.FindForMemberAsync(groupId, userId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            Result changeable = found.Value.CheckCanBeChangedBy(userId, _timeProvider.GetUtcNow());
            if (!changeable.IsSuccess)
            {
                return changeable;
            }

            GroupRoster roster = await _members.RosterOfAsync(groupId, cancellationToken);
            NamedMember ownRow = roster.CurrentRowOf(userId)
                ?? throw new InvalidOperationException($"User {userId} was found as a current member of group {groupId}, but has no current row in it.");
            ownRow.Member.SetCountsInBudget(countsInBudget);

            return Result.Ok();
        }
    }
}
