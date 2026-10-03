using System.Text.Json;

namespace Kvit.Domain.Entities
{
    public class ActivityEvent
    {
        private ActivityEvent()
        {
        }

        public Guid Id { get; private set; }

        public Guid GroupId { get; private set; }

        public Guid ActorUserId { get; private set; }

        public ActivityEventType Type { get; private set; }

        public Guid? ExpenseId { get; private set; }

        public Guid? SettlementId { get; private set; }

        public Guid? MemberId { get; private set; }

        public string? Changes { get; private set; }

        public string? Data { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public static ActivityEvent GroupCreated(Guid groupId, Guid actorUserId, string groupName, DateTimeOffset createdAt)
        {
            ActivityEvent activityEvent = New(groupId, actorUserId, ActivityEventType.GroupCreated, createdAt);
            activityEvent.Data = JsonSerializer.Serialize(new { name = groupName });

            return activityEvent;
        }

        public static ActivityEvent GroupRenamed(Guid groupId, Guid actorUserId, FieldChange nameChange, DateTimeOffset createdAt)
        {
            return WithChanges(groupId, actorUserId, ActivityEventType.GroupRenamed, [nameChange], createdAt);
        }

        public static ActivityEvent GroupSettingsChanged(Guid groupId, Guid actorUserId, IReadOnlyList<FieldChange> settingsChanges, DateTimeOffset createdAt)
        {
            return WithChanges(groupId, actorUserId, ActivityEventType.GroupSettingsChanged, settingsChanges, createdAt);
        }

        public static ActivityEvent GroupDeleted(Guid groupId, Guid actorUserId, DateTimeOffset createdAt)
        {
            return New(groupId, actorUserId, ActivityEventType.GroupDeleted, createdAt);
        }

        public static ActivityEvent GroupRestored(Guid groupId, Guid actorUserId, DateTimeOffset createdAt)
        {
            return New(groupId, actorUserId, ActivityEventType.GroupRestored, createdAt);
        }

        private static ActivityEvent WithChanges(Guid groupId, Guid actorUserId, ActivityEventType type, IReadOnlyList<FieldChange> changes, DateTimeOffset createdAt)
        {
            ActivityEvent activityEvent = New(groupId, actorUserId, type, createdAt);
            activityEvent.Changes = JsonSerializer.Serialize(changes, JsonSerializerOptions.Web);

            return activityEvent;
        }

        private static ActivityEvent New(Guid groupId, Guid actorUserId, ActivityEventType type, DateTimeOffset createdAt)
        {
            return new ActivityEvent
            {
                Id = Guid.CreateVersion7(),
                GroupId = groupId,
                ActorUserId = actorUserId,
                Type = type,
                CreatedAt = createdAt,
            };
        }
    }
}
