using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class UndoInviteReset(
        IGroupRepository _groups,
        IActivityEventRepository _activityEvents,
        TimeProvider _timeProvider)
    {
        public async Task<Result<string>> Execute(Guid groupId, Guid userId, CancellationToken cancellationToken)
        {
            Result<Group> found = await _groups.FindForOwnerAsync(groupId, userId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found.ToFailure<string>();
            }

            Group group = found.Value;
            DateTimeOffset now = _timeProvider.GetUtcNow();
            Result undone = group.UndoInviteReset(now);
            if (!undone.IsSuccess)
            {
                return undone.ToFailure<string>();
            }

            _activityEvents.Add(ActivityEvent.InviteLinkRestored(groupId, userId, now));

            return Result.Ok(group.InviteToken);
        }
    }
}
