using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class SetGroupPlan(
        IGroupRepository _groups,
        IActivityEventRepository _activityEvents,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid groupId, Guid userId, long? amountMinor, string? currency, CancellationToken cancellationToken)
        {
            Result<Group> found = await _groups.FindForMemberAsync(groupId, userId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            Group group = found.Value;
            Result owner = group.CheckOwner(userId);
            if (!owner.IsSuccess)
            {
                return owner;
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            Result changeable = group.CheckCanBeChangedBy(userId, now);
            if (!changeable.IsSuccess)
            {
                return changeable;
            }

            Result<Money> plan = Group.CheckPlan(amountMinor, currency);
            if (!plan.IsSuccess)
            {
                return plan;
            }

            if (group.SetPlan(plan.Value))
            {
                _activityEvents.Add(ActivityEvent.GroupPlanSet(group.Id, userId, plan.Value, now));
            }

            return Result.Ok();
        }
    }
}
