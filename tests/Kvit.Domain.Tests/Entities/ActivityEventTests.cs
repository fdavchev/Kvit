using System.Text.Json;
using Kvit.Domain.Entities;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class ActivityEventTests
    {
        private static readonly Guid GroupId = Guid.Parse("0199a1b2-c3d4-7e5f-8a6b-7c8d9e0f1a30");

        [Fact]
        public void GroupCreated_SetsTypeGroupActorAndMoment()
        {
            ActivityEvent activityEvent = ActivityEvent.GroupCreated(GroupId, GroupTestData.OwnerId, GroupTestData.ValidName, GroupTestData.Moment);

            Assert.NotEqual(Guid.Empty, activityEvent.Id);
            Assert.Equal(ActivityEventType.GroupCreated, activityEvent.Type);
            Assert.Equal(GroupId, activityEvent.GroupId);
            Assert.Equal(GroupTestData.OwnerId, activityEvent.ActorUserId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
        }

        [Theory]
        [InlineData("Greece trip")]
        [InlineData("Патување во \"Грција\"")]
        public void GroupCreated_PutsTheGroupNameInTheDataAndLeavesChangesEmpty(string groupName)
        {
            ActivityEvent activityEvent = ActivityEvent.GroupCreated(GroupId, GroupTestData.OwnerId, groupName, GroupTestData.Moment);

            Assert.NotNull(activityEvent.Data);
            JsonElement data = JsonDocument.Parse(activityEvent.Data).RootElement;
            Assert.Equal(["name"], data.EnumerateObject().Select(property => property.Name));
            Assert.Equal(groupName, data.GetProperty("name").GetString());
            Assert.Null(activityEvent.Changes);
        }

        [Fact]
        public void GroupRenamed_PutsTheNameChangeInChangesAndLeavesDataEmpty()
        {
            ActivityEvent activityEvent = ActivityEvent.GroupRenamed(GroupId, GroupTestData.OwnerId, new FieldChange("name", "Greece trip", "Rome"), GroupTestData.Moment);

            Assert.Equal(ActivityEventType.GroupRenamed, activityEvent.Type);
            Assert.Equal(GroupId, activityEvent.GroupId);
            Assert.Equal(GroupTestData.OwnerId, activityEvent.ActorUserId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
            Assert.Equal([("name", "Greece trip", "Rome")], ReadChanges(activityEvent));
            Assert.Null(activityEvent.Data);
        }

        [Fact]
        public void GroupSettingsChanged_ListsEveryGivenChangeWithItsFieldName()
        {
            FieldChange[] settingsChanges = [new FieldChange("emoji", "\U0001F3D6", "\U0001F37D"), new FieldChange("defaultCurrency", "MKD", "EUR")];

            ActivityEvent activityEvent = ActivityEvent.GroupSettingsChanged(GroupId, GroupTestData.OwnerId, settingsChanges, GroupTestData.Moment);

            Assert.Equal(ActivityEventType.GroupSettingsChanged, activityEvent.Type);
            Assert.Equal([("emoji", "\U0001F3D6", "\U0001F37D"), ("defaultCurrency", "MKD", "EUR")], ReadChanges(activityEvent));
            Assert.Null(activityEvent.Data);
        }

        [Fact]
        public void GroupDeleted_HasTheTypeAndNeitherChangesNorData()
        {
            ActivityEvent activityEvent = ActivityEvent.GroupDeleted(GroupId, GroupTestData.OwnerId, GroupTestData.Moment);

            Assert.Equal(ActivityEventType.GroupDeleted, activityEvent.Type);
            Assert.Equal(GroupId, activityEvent.GroupId);
            Assert.Equal(GroupTestData.OwnerId, activityEvent.ActorUserId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
            Assert.Null(activityEvent.Changes);
            Assert.Null(activityEvent.Data);
        }

        [Fact]
        public void GroupRestored_HasTheTypeAndNeitherChangesNorData()
        {
            ActivityEvent activityEvent = ActivityEvent.GroupRestored(GroupId, GroupTestData.OwnerId, GroupTestData.Moment);

            Assert.Equal(ActivityEventType.GroupRestored, activityEvent.Type);
            Assert.Equal(GroupId, activityEvent.GroupId);
            Assert.Equal(GroupTestData.OwnerId, activityEvent.ActorUserId);
            Assert.Equal(GroupTestData.Moment, activityEvent.CreatedAt);
            Assert.Null(activityEvent.Changes);
            Assert.Null(activityEvent.Data);
        }

        [Fact]
        public void GroupEvents_AreNotLinkedToAnExpenseASettlementOrAMember()
        {
            ActivityEvent activityEvent = ActivityEvent.GroupDeleted(GroupId, GroupTestData.OwnerId, GroupTestData.Moment);

            Assert.Null(activityEvent.ExpenseId);
            Assert.Null(activityEvent.SettlementId);
            Assert.Null(activityEvent.MemberId);
        }

        private static List<(string Field, string Old, string New)> ReadChanges(ActivityEvent activityEvent)
        {
            Assert.NotNull(activityEvent.Changes);
            JsonElement changes = JsonDocument.Parse(activityEvent.Changes).RootElement;
            List<(string Field, string Old, string New)> entries = [];
            foreach (JsonElement entry in changes.EnumerateArray())
            {
                Assert.Equal(["field", "old", "new"], entry.EnumerateObject().Select(property => property.Name));
                entries.Add((entry.GetProperty("field").GetString()!, entry.GetProperty("old").GetString()!, entry.GetProperty("new").GetString()!));
            }

            return entries;
        }
    }
}
