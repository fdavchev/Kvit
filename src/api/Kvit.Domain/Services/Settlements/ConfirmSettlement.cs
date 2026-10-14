using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;

namespace Kvit.Domain.Services.Settlements
{
    public sealed class ConfirmSettlement(
        IGroupRepository _groups,
        IGroupMemberRepository _members,
        ISettlementRepository _settlements,
        IActivityEventRepository _activityEvents,
        IUsageEventRepository _usageEvents,
        CancelClosingOnChange _cancelClosingOnChange,
        FinishPaidOneBill _finishPaidOneBill,
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
            GroupRoster roster = await _members.RosterOfAsync(groupId, cancellationToken);
            DateTimeOffset now = _timeProvider.GetUtcNow();
            Group group = found.Value;
            Result confirmed = settlement.Confirm(roster, userId, group.OwnerUserId, now);
            if (!confirmed.IsSuccess)
            {
                return confirmed;
            }

            Result changeable = await _cancelClosingOnChange.Execute(group, userId, cancellationToken);
            if (!changeable.IsSuccess)
            {
                return changeable;
            }

            _activityEvents.Add(ActivityEvent.SettlementConfirmed(userId, settlement, roster, now));
            _usageEvents.Add(UsageEvent.SettlementConfirmed(userId, now));
            await _finishPaidOneBill.Execute(group, userId, cancellationToken);

            return Result.Ok();
        }
    }
}
