using System.Text.Json;
using Kvit.Domain.MoneyRules;

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

        public static ActivityEvent InviteLinkReset(Guid groupId, Guid actorUserId, DateTimeOffset createdAt)
        {
            return New(groupId, actorUserId, ActivityEventType.InviteLinkReset, createdAt);
        }

        public static ActivityEvent InviteLinkRestored(Guid groupId, Guid actorUserId, DateTimeOffset createdAt)
        {
            return New(groupId, actorUserId, ActivityEventType.InviteLinkRestored, createdAt);
        }

        public static ActivityEvent MemberAdded(Guid groupId, Guid actorUserId, Guid memberId, string memberName, DateTimeOffset createdAt)
        {
            return AboutMember(groupId, actorUserId, ActivityEventType.MemberAdded, memberId, new { name = memberName }, createdAt);
        }

        public static ActivityEvent MemberJoined(Guid groupId, Guid actorUserId, Guid memberId, string memberName, DateTimeOffset createdAt)
        {
            return AboutMember(groupId, actorUserId, ActivityEventType.MemberJoined, memberId, new { name = memberName }, createdAt);
        }

        public static ActivityEvent MemberRemoved(Guid groupId, Guid actorUserId, Guid memberId, string memberName, DateTimeOffset createdAt)
        {
            return AboutMember(groupId, actorUserId, ActivityEventType.MemberRemoved, memberId, new { name = memberName }, createdAt);
        }

        public static ActivityEvent MemberLeft(Guid groupId, Guid actorUserId, Guid memberId, string memberName, DateTimeOffset createdAt)
        {
            return AboutMember(groupId, actorUserId, ActivityEventType.MemberLeft, memberId, new { name = memberName }, createdAt);
        }

        public static ActivityEvent MemberLetBackIn(Guid groupId, Guid actorUserId, Guid memberId, string memberName, DateTimeOffset createdAt)
        {
            return AboutMember(groupId, actorUserId, ActivityEventType.MemberLetBackIn, memberId, new { name = memberName }, createdAt);
        }

        public static ActivityEvent OwnershipTransferred(Guid groupId, Guid actorUserId, Guid newOwnerMemberId, string newOwnerName, DateTimeOffset createdAt)
        {
            return AboutMember(groupId, actorUserId, ActivityEventType.OwnershipTransferred, newOwnerMemberId, new { name = newOwnerName }, createdAt);
        }

        public static ActivityEvent MemberClaimed(Guid groupId, Guid actorUserId, Guid memberId, string memberName, string claimedName, DateTimeOffset createdAt)
        {
            return AboutMember(groupId, actorUserId, ActivityEventType.MemberClaimed, memberId, new { name = memberName, claimedName }, createdAt);
        }

        public static ActivityEvent ClaimUndone(Guid groupId, Guid actorUserId, Guid memberId, string memberName, string claimedName, DateTimeOffset createdAt)
        {
            return AboutMember(groupId, actorUserId, ActivityEventType.ClaimUndone, memberId, new { name = memberName, claimedName }, createdAt);
        }

        public static ActivityEvent ExpenseAdded(Guid groupId, Guid actorUserId, Guid expenseId, string? title, long amountMinor, Currency currency, DateTimeOffset createdAt)
        {
            ActivityEvent activityEvent = AboutExpense(groupId, actorUserId, ActivityEventType.ExpenseAdded, expenseId, createdAt);
            activityEvent.Data = ExpenseFacts(title, amountMinor, currency);

            return activityEvent;
        }

        public static ActivityEvent ExpenseEdited(Guid groupId, Guid actorUserId, Guid expenseId, IReadOnlyList<FieldChange> changes, DateTimeOffset createdAt)
        {
            ActivityEvent activityEvent = WithChanges(groupId, actorUserId, ActivityEventType.ExpenseEdited, changes, createdAt);
            activityEvent.ExpenseId = expenseId;

            return activityEvent;
        }

        public static ActivityEvent ExpenseDeleted(Guid groupId, Guid actorUserId, Guid expenseId, string? title, long amountMinor, Currency currency, DateTimeOffset createdAt)
        {
            ActivityEvent activityEvent = AboutExpense(groupId, actorUserId, ActivityEventType.ExpenseDeleted, expenseId, createdAt);
            activityEvent.Data = ExpenseFacts(title, amountMinor, currency);

            return activityEvent;
        }

        public static ActivityEvent ExpenseRestored(Guid groupId, Guid actorUserId, Guid expenseId, DateTimeOffset createdAt)
        {
            return AboutExpense(groupId, actorUserId, ActivityEventType.ExpenseRestored, expenseId, createdAt);
        }

        private static ActivityEvent AboutExpense(Guid groupId, Guid actorUserId, ActivityEventType type, Guid expenseId, DateTimeOffset createdAt)
        {
            ActivityEvent activityEvent = New(groupId, actorUserId, type, createdAt);
            activityEvent.ExpenseId = expenseId;

            return activityEvent;
        }

        private static string ExpenseFacts(string? title, long amountMinor, Currency currency)
        {
            return JsonSerializer.Serialize(new { title, amountMinor, currency = currency.ToString() });
        }

        private static ActivityEvent AboutMember(Guid groupId, Guid actorUserId, ActivityEventType type, Guid memberId, object data, DateTimeOffset createdAt)
        {
            ActivityEvent activityEvent = New(groupId, actorUserId, type, createdAt);
            activityEvent.MemberId = memberId;
            activityEvent.Data = JsonSerializer.Serialize(data);

            return activityEvent;
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
