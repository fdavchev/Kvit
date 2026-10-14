using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.MoneyRules;

namespace Kvit.Domain.Services.Groups
{
    public sealed class FinishPaidOneBill(
        IGroupMemberRepository _members,
        IGroupMoneyRepository _money,
        IActivityEventRepository _activityEvents,
        TimeProvider _timeProvider)
    {
        public async Task Execute(Group group, Guid userId, CancellationToken cancellationToken)
        {
            if (group.Kind != GroupKind.OneBill)
            {
                return;
            }

            await _members.SaveAsync(cancellationToken);
            GroupMoney money = await _money.MoneyOfAsync(group.Id, cancellationToken);
            if (!money.IsPaidOff())
            {
                return;
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            group.Finish(now);
            _activityEvents.Add(ActivityEvent.GroupFinished(group.Id, userId, now));
        }
    }
}
