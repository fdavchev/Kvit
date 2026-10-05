using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class ResetInviteLink(
        IGroupRepository _groups,
        IActivityEventRepository _activityEvents,
        IInviteTokenGenerator _inviteTokenGenerator,
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
            group.ResetInvite(_inviteTokenGenerator.NewToken(), now);
            _activityEvents.Add(ActivityEvent.InviteLinkReset(groupId, userId, now));

            return Result.Ok(group.InviteToken);
        }
    }
}
