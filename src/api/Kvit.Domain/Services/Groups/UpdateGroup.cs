using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class UpdateGroup(
        IGroupRepository _groups,
        IActivityEventRepository _activityEvents,
        TimeProvider _timeProvider)
    {
        public async Task<Result> Execute(Guid groupId, Guid userId, string name, string emoji, string currency, CancellationToken cancellationToken)
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

            Result<GroupChanges> changes = group.Update(name, emoji, currency);
            if (!changes.IsSuccess)
            {
                return changes;
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            if (changes.Value.Name is FieldChange nameChange)
            {
                _activityEvents.Add(ActivityEvent.GroupRenamed(group.Id, userId, nameChange, now));
            }

            if (changes.Value.Settings.Count > 0)
            {
                _activityEvents.Add(ActivityEvent.GroupSettingsChanged(group.Id, userId, changes.Value.Settings, now));
            }

            return Result.Ok();
        }
    }
}
