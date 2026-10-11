using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Kvit.Domain.Settlements;

namespace Kvit.Domain.Services.Settlements
{
    public sealed class RecordSettlement(
        IGroupRepository _groups,
        IGroupMemberRepository _members,
        ISettlementRepository _settlements,
        IActivityEventRepository _activityEvents,
        IUsageEventRepository _usageEvents,
        TimeProvider _timeProvider)
    {
        public async Task<Result<Guid>> Execute(Guid groupId, Guid userId, Guid clientRequestId, SettlementInput input, CancellationToken cancellationToken)
        {
            Result<Group> found = await _groups.FindForMemberAsync(groupId, userId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found.ToFailure<Guid>();
            }

            Settlement? earlier = await _settlements.FindByClientRequestIdAsync(clientRequestId, cancellationToken);
            if (earlier is not null)
            {
                return earlier.GroupId == groupId && earlier.RecordedByUserId == userId
                    ? Result.Ok(earlier.Id)
                    : Settlement.ClientRequestIdUsed<Guid>(clientRequestId);
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            GroupRoster roster = await _members.RosterOfAsync(groupId, cancellationToken);
            Result<Settlement> created = Settlement.Create(groupId, input, roster, userId, found.Value.OwnerUserId, clientRequestId, now);
            if (!created.IsSuccess)
            {
                return created.ToFailure<Guid>();
            }

            Settlement settlement = created.Value;
            _settlements.Add(settlement);
            _activityEvents.Add(ActivityEvent.SettlementRecorded(userId, settlement, roster, now));
            if (settlement.Status == SettlementStatus.Confirmed)
            {
                _usageEvents.Add(UsageEvent.SettlementConfirmed(userId, now));
            }

            return Result.Ok(settlement.Id);
        }
    }
}
