using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.AdminActions
{
    public sealed class CloseGroupAsAdmin(
        IGroupRepository _groups,
        IAdminActionRepository _adminActions,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid adminUserId, Guid groupId, string reason, CancellationToken cancellationToken)
        {
            Result<Group> found = await _groups.LockAnyIncludingDeletedAsync(groupId, cancellationToken);
            if (!found.IsSuccess)
            {
                return found;
            }

            Group group = found.Value;
            DateTimeOffset now = _timeProvider.GetUtcNow();
            if (group.CloseByAdmin(now))
            {
                _adminActions.Add(AdminAction.AboutGroup(adminUserId, AdminActionType.GroupClosed, group.Id, group.Name, reason, now));
            }

            return Result.Ok();
        }
    }
}
