using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class UnlockGroup(
        IGroupRepository _groups,
        IClosingResponseRepository _closingResponses,
        IActivityEventRepository _activityEvents,
        FinishExpiredClosing _finishExpiredClosing,
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
            DateTimeOffset now = _timeProvider.GetUtcNow();
            _finishExpiredClosing.Execute(group, now);
            Result canUnlock = group.CheckCanUnlock(userId, now);
            if (!canUnlock.IsSuccess)
            {
                return canUnlock;
            }

            await _closingResponses.RemoveAllOfAsync(groupId, cancellationToken);
            group.Unlock();
            _activityEvents.Add(ActivityEvent.GroupReopened(groupId, userId, now));

            return Result.Ok();
        }
    }
}
