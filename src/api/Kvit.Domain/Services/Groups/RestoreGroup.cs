using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class RestoreGroup(
        IGroupRepository _groups,
        IActivityEventRepository _activityEvents,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid groupId, Guid userId, CancellationToken cancellationToken)
        {
            Result<Group> found = await _groups.FindIncludingDeletedForMemberAsync(groupId, userId, cancellationToken);
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
            Result restored = group.Restore(now);
            if (!restored.IsSuccess)
            {
                return restored;
            }

            _activityEvents.Add(ActivityEvent.GroupRestored(group.Id, userId, now));

            return Result.Ok();
        }
    }
}
