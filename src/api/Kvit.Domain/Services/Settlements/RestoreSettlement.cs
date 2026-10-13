using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Settlements
{
    public sealed class RestoreSettlement(
        IGroupRepository _groups,
        IGroupMemberRepository _members,
        ISettlementRepository _settlements,
        IActivityEventRepository _activityEvents,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid groupId, Guid userId, Guid settlementId, CancellationToken cancellationToken)
        {
            Result<Group> found = await _groups.FindForMemberAsync(groupId, userId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            Result<Settlement> foundSettlement = await _settlements.FindIncludingDeletedInGroupAsync(groupId, settlementId, cancellationToken);
            if (!foundSettlement.IsSuccess)
            {
                return foundSettlement;
            }

            Settlement settlement = foundSettlement.Value;
            GroupRoster roster = await _members.RosterOfAsync(groupId, cancellationToken);
            Result restored = settlement.Restore(roster, userId, found.Value.OwnerUserId);
            if (!restored.IsSuccess)
            {
                return restored;
            }

            _activityEvents.Add(ActivityEvent.SettlementRestored(userId, settlement, roster, _timeProvider.GetUtcNow()));

            return Result.Ok();
        }
    }
}
