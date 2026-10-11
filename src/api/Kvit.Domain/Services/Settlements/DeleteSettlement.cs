using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Settlements
{
    public sealed class DeleteSettlement(
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

            Result<Settlement> foundSettlement = await _settlements.FindInGroupAsync(groupId, settlementId, cancellationToken);
            if (!foundSettlement.IsSuccess)
            {
                return foundSettlement;
            }

            Settlement settlement = foundSettlement.Value;
            DateTimeOffset now = _timeProvider.GetUtcNow();
            Result deleted = settlement.Delete(userId, found.Value.OwnerUserId, now);
            if (!deleted.IsSuccess)
            {
                return deleted;
            }

            GroupRoster roster = await _members.RosterOfAsync(groupId, cancellationToken);
            _activityEvents.Add(ActivityEvent.SettlementDeleted(userId, settlement, roster, now));

            return Result.Ok();
        }
    }
}
