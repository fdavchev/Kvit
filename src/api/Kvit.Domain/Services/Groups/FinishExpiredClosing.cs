using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;

namespace Kvit.Domain.Services.Groups
{
    public sealed class FinishExpiredClosing(IActivityEventRepository _activityEvents)
    {
        public void Execute(Group group, DateTimeOffset now)
        {
            if (group.FinishIfClosingRanOut(now) is DateTimeOffset finishedAt)
            {
                _activityEvents.Add(ActivityEvent.GroupFinished(group.Id, group.OwnerUserId, finishedAt));
            }
        }
    }
}
