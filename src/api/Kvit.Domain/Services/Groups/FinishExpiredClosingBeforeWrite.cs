using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class FinishExpiredClosingBeforeWrite(
        IGroupRepository _groups,
        FinishExpiredClosing _finishExpiredClosing,
        TimeProvider _timeProvider)
    {
        public async Task ExecuteForMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken)
        {
            Result<Group> found = await _groups.FindIncludingDeletedForMemberAsync(groupId, userId, cancellationToken);
            if (found.IsSuccess)
            {
                _finishExpiredClosing.Execute(found.Value, _timeProvider.GetUtcNow());
            }
        }

        public async Task ExecuteForInviteAsync(string inviteToken, CancellationToken cancellationToken)
        {
            Result<Group> found = await _groups.FindByInviteTokenAsync(inviteToken, cancellationToken);
            if (found.IsSuccess)
            {
                _finishExpiredClosing.Execute(found.Value, _timeProvider.GetUtcNow());
            }
        }
    }
}
