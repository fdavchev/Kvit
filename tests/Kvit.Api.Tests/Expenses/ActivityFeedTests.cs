using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class ActivityFeedTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const int FeedLimit = 200;
        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        [Fact]
        public async Task Activity_GroupJustCreated_AnswersOnlyAnEventsArrayWithTheGroupCreatedEvent()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            HttpResponseMessage response = await ExpenseRequests.ActivityAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(["events"], GroupRequests.PropertyNamesOf(body));
            Assert.Equal(["GroupCreated"], ExpenseJson.TypesOf(body.GetProperty("events")));
        }

        [Fact]
        public async Task Activity_Event_AnswersExactlyTheNineFields()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            JsonElement events = await ReadEventsAsync(group.Owner, group.GroupId);

            JsonElement entry = Assert.Single(events.EnumerateArray());
            Assert.Equal(ExpenseJson.ActivityFieldNames, GroupRequests.PropertyNamesOf(entry));
        }

        [Fact]
        public async Task Activity_GroupCreatedEvent_AnswersTheActorTheNameDataAndNoChanges()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            JsonElement events = await ReadEventsAsync(group.Member, group.GroupId);

            JsonElement entry = events[0];
            Assert.Equal(group.Owner.UserId, entry.GetProperty("actorUserId").GetGuid());
            Assert.Equal(GroupsApp.OwnerName, entry.GetProperty("actorName").GetString());
            Assert.Equal(JsonValueKind.Null, entry.GetProperty("expenseId").ValueKind);
            Assert.Equal(JsonValueKind.Null, entry.GetProperty("memberId").ValueKind);
            Assert.Equal(JsonValueKind.Null, entry.GetProperty("changes").ValueKind);
            Assert.Equal(JsonValueKind.Object, entry.GetProperty("data").ValueKind);
            Assert.NotEqual(Guid.Empty, entry.GetProperty("id").GetGuid());
        }

        [Fact]
        public async Task Activity_ExpenseEvents_AreNewestFirstWithTheExpenseAndTheActor()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId) with { Title = "Dinner" };
            _app.Clock.Advance(OneMinute);
            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, input);
            _app.Clock.Advance(OneMinute);
            await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { Title = "Lunch" });
            _app.Clock.Advance(OneMinute);
            await ExpenseRequests.DeleteAsync(group.Member.Client, group.GroupId, expenseId);
            _app.Clock.Advance(OneMinute);
            await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId);

            JsonElement events = await ReadEventsAsync(group.Owner, group.GroupId);

            Assert.Equal(["ExpenseRestored", "ExpenseDeleted", "ExpenseEdited", "ExpenseAdded", "GroupCreated"], ExpenseJson.TypesOf(events));
            Assert.Equal([expenseId, expenseId, expenseId, expenseId], [.. events.EnumerateArray().Take(4).Select(entry => entry.GetProperty("expenseId").GetGuid())]);
            Assert.Equal(["Ana", "Bojan", "Ana", "Bojan", "Ana"], [.. events.EnumerateArray().Select(entry => entry.GetProperty("actorName").GetString()!)]);
        }

        [Fact]
        public async Task Activity_ExpenseAddedEvent_AnswersTheDataObjectWithTitleAmountAndCurrency()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(1_550, "EUR", group.OwnerRowId, group.OwnerRowId) with { Title = "Dinner" });

            JsonElement events = await ReadEventsAsync(group.Owner, group.GroupId);

            JsonElement entry = events[0];
            Assert.Equal("ExpenseAdded", entry.GetProperty("type").GetString());
            Assert.Equal(JsonValueKind.Null, entry.GetProperty("changes").ValueKind);
            JsonElement data = entry.GetProperty("data");
            Assert.Equal("Dinner", data.GetProperty("title").GetString());
            Assert.Equal(1_550, data.GetProperty("amountMinor").GetInt64());
            Assert.Equal("EUR", data.GetProperty("currency").GetString());
        }

        [Fact]
        public async Task Activity_ExpenseDeletedEvent_AnswersTheDataObjectWithTitleAmountAndCurrency()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(1_550, "EUR", group.OwnerRowId, group.OwnerRowId) with { Title = "Dinner" });
            _app.Clock.Advance(OneMinute);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            JsonElement events = await ReadEventsAsync(group.Owner, group.GroupId);

            JsonElement data = events[0].GetProperty("data");
            Assert.Equal("ExpenseDeleted", events[0].GetProperty("type").GetString());
            Assert.Equal("Dinner", data.GetProperty("title").GetString());
            Assert.Equal(1_550, data.GetProperty("amountMinor").GetInt64());
            Assert.Equal("EUR", data.GetProperty("currency").GetString());
        }

        [Fact]
        public async Task Activity_ExpenseEditedEvent_AnswersTheParsedChangesList()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId) with { Title = "Dinner" };
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            _app.Clock.Advance(OneMinute);
            await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { Title = "Lunch", Note = "Sea" });

            JsonElement events = await ReadEventsAsync(group.Owner, group.GroupId);

            JsonElement entry = events[0];
            Assert.Equal("ExpenseEdited", entry.GetProperty("type").GetString());
            JsonElement changes = entry.GetProperty("changes");
            Assert.Equal(JsonValueKind.Array, changes.ValueKind);
            Assert.All(changes.EnumerateArray(), change => Assert.Equal(["field", "new", "old"], GroupRequests.PropertyNamesOf(change)));
            JsonElement title = changes.EnumerateArray().Single(change => change.GetProperty("field").GetString() == "title");
            Assert.Equal("Dinner", title.GetProperty("old").GetString());
            Assert.Equal("Lunch", title.GetProperty("new").GetString());
        }

        [Fact]
        public async Task Activity_MemberEvents_AnswerTheMemberIdAndTheNameAsData()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            _app.Clock.Advance(OneMinute);
            HttpResponseMessage added = await GroupRequests.AddMemberAsync(group.Owner.Client, group.GroupId, "Marko");
            Guid markoId = (await AuthRequests.ReadJsonAsync(added)).GetProperty("id").GetGuid();

            JsonElement events = await ReadEventsAsync(group.Owner, group.GroupId);

            JsonElement entry = events[0];
            Assert.Equal("MemberAdded", entry.GetProperty("type").GetString());
            Assert.Equal(markoId, entry.GetProperty("memberId").GetGuid());
            Assert.Equal("Marko", entry.GetProperty("data").GetProperty("name").GetString());
        }

        [Fact]
        public async Task Activity_ActorWhoRenamedTheirAccount_IsShownByTheCurrentName()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await UserRows.SetDisplayNameAsync(_app, group.Owner.UserId, "Ana Ivanova");

            JsonElement events = await ReadEventsAsync(group.Member, group.GroupId);

            Assert.Equal("Ana Ivanova", events[0].GetProperty("actorName").GetString());
        }

        [Fact]
        public async Task Activity_MoreThan200Events_AnswersTheLatest200NewestFirst()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await InsertEventsAsync(group, 205);

            JsonElement events = await ReadEventsAsync(group.Owner, group.GroupId);

            Assert.Equal(FeedLimit, events.GetArrayLength());
            List<DateTimeOffset> times = [.. events.EnumerateArray().Select(entry => entry.GetProperty("createdAt").GetDateTimeOffset())];
            Assert.Equal([.. times.OrderByDescending(time => time)], times);
            Assert.Equal(new DateTimeOffset(2030, 1, 1, 0, 3, 25, TimeSpan.Zero), times[0]);
            Assert.Equal(new DateTimeOffset(2030, 1, 1, 0, 0, 6, TimeSpan.Zero), times[^1]);
        }

        [Fact]
        public async Task Activity_Exactly200Events_AnswersAllOfThem()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await InsertEventsAsync(group, FeedLimit - 1);

            JsonElement events = await ReadEventsAsync(group.Owner, group.GroupId);

            Assert.Equal(FeedLimit, events.GetArrayLength());
            Assert.Contains("GroupCreated", ExpenseJson.TypesOf(events));
        }

        [Fact]
        public async Task Activity_EventsOfAnotherGroup_AreNotInTheAnswer()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid otherGroupId = await _app.CreateGroupAsync(group.Owner);
            Guid ownerRowInOther = await GroupRows.CurrentMemberIdAsync(_app, otherGroupId, group.Owner.UserId);
            await _app.AddExpenseAsync(group.Owner, otherGroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", ownerRowInOther, ownerRowInOther));

            JsonElement events = await ReadEventsAsync(group.Owner, group.GroupId);

            Assert.Equal(["GroupCreated"], ExpenseJson.TypesOf(events));
        }

        [Fact]
        public async Task Activity_EveryMemberCanReadTheFeed()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            HttpResponseMessage response = await ExpenseRequests.ActivityAsync(group.Member.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private async Task<JsonElement> ReadEventsAsync(SignedInUser viewer, Guid groupId)
        {
            HttpResponseMessage response = await ExpenseRequests.ActivityAsync(viewer.Client, groupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await AuthRequests.ReadJsonAsync(response)).GetProperty("events");
        }

        private Task InsertEventsAsync(ExpenseGroup group, int count)
        {
            return _app.ExecuteAsync(
                "INSERT INTO activity_events (id, group_id, actor_user_id, type, created_at) "
                + "SELECT gen_random_uuid(), @group_id, @actor, 'GroupRenamed', timestamptz '2030-01-01 00:00:00+00' + n * interval '1 second' "
                + "FROM generate_series(1, @count) AS n",
                new NpgsqlParameter("group_id", group.GroupId),
                new NpgsqlParameter("actor", group.Owner.UserId),
                new NpgsqlParameter("count", count));
        }
    }
}
