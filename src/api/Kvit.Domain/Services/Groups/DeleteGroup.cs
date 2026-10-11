using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class DeleteGroup(
        IGroupRepository _groups,
        IGroupMoneyRepository _money,
        IActivityEventRepository _activityEvents,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid groupId, Guid userId, CancellationToken cancellationToken)
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

            GroupMoney money = await _money.MoneyOfAsync(group.Id, cancellationToken);
            Result settled = money.CheckEveryoneSettled();
            if (!settled.IsSuccess)
            {
                return settled;
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            group.Delete(userId, now);
            _activityEvents.Add(ActivityEvent.GroupDeleted(group.Id, userId, now));

            return Result.Ok();
        }
    }
}
