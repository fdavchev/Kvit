using System.Text.Json;
using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Dashboard
{
    public class DashboardRecentActivityTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string Greece = "Greece";
        private const string Flat = "Flat";

        [Fact]
        public async Task RecentActivity_NoGroup_IsEmpty()
        {
            SignedInUser user = await _app.SignUpAsync();

            JsonElement body = await DashboardRequests.ReadAsync(user);

            Assert.Equal(0, body.GetProperty("recentActivity").GetArrayLength());
        }

        [Fact]
        public async Task RecentActivity_FewerThanFiveEvents_AnswersAllOfThemNewestFirst()
        {
            SignedInUser ana = await _app.SignUpAsync();
            ExpenseGroup group = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, Greece);
            await DashboardSteps.AddOwnExpenseAsync(_app, group.GroupId, ana, group.OwnerRowId, "Dinner");

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            Assert.Equal(["ExpenseAdded", "GroupCreated"], DashboardJson.ActivityTypesOf(body));
        }

        [Fact]
        public async Task RecentActivity_Row_AnswersExactlyTheElevenFields()
        {
            SignedInUser ana = await _app.SignUpAsync();
            await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, Greece);

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            JsonElement row = Assert.Single(body.GetProperty("recentActivity").EnumerateArray());
            Assert.Equal(DashboardJson.ActivityRowFieldNames, GroupRequests.PropertyNamesOf(row));
        }

        [Fact]
        public async Task RecentActivity_ExpenseAddedRow_AnswersTheSameValuesAsTheGroupsOwnFeedPlusTheGroup()
        {
            SignedInUser ana = await _app.SignUpAsync();
            ExpenseGroup group = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, Greece);
            Guid expenseId = await DashboardSteps.AddOwnExpenseAsync(_app, group.GroupId, ana, group.OwnerRowId, "Dinner");

            JsonElement body = await DashboardRequests.ReadAsync(ana);
            JsonElement groupFeed = await SettlementSteps.ReadActivityAsync(ana, group.GroupId);

            JsonElement row = body.GetProperty("recentActivity")[0];
            JsonElement feedRow = groupFeed[0];
            Assert.Equal("ExpenseAdded", row.GetProperty("type").GetString());
            Assert.Equal(feedRow.GetProperty("id").GetGuid(), row.GetProperty("id").GetGuid());
            Assert.Equal(ana.UserId, row.GetProperty("actorUserId").GetGuid());
            Assert.Equal(GroupsApp.OwnerName, row.GetProperty("actorName").GetString());
            Assert.Equal(expenseId, row.GetProperty("expenseId").GetGuid());
            Assert.Equal(JsonValueKind.Null, row.GetProperty("memberId").ValueKind);
            Assert.Equal(JsonValueKind.Null, row.GetProperty("changes").ValueKind);
            Assert.Equal("Dinner", row.GetProperty("data").GetProperty("title").GetString());
            Assert.Equal(ExpenseInputs.OneThousandMkd, row.GetProperty("data").GetProperty("amountMinor").GetInt64());
            Assert.Equal("MKD", row.GetProperty("data").GetProperty("currency").GetString());
            Assert.Equal(feedRow.GetProperty("createdAt").GetDateTimeOffset(), row.GetProperty("createdAt").GetDateTimeOffset());
            Assert.Equal(group.GroupId, row.GetProperty("groupId").GetGuid());
            Assert.Equal(Greece, row.GetProperty("groupName").GetString());
        }

        [Fact]
        public async Task RecentActivity_SixEventsInTwoGroups_AnswersTheLatestFiveNewestFirstWithOwnEventsIncluded()
        {
            ActivityScenario scenario = await CreateScenarioAsync();

            JsonElement body = await DashboardRequests.ReadAsync(scenario.Ana);

            Assert.Equal(["ExpenseAdded", "ExpenseAdded", "ExpenseAdded", "ExpenseAdded", "GroupCreated"], DashboardJson.ActivityTypesOf(body));
            Assert.Equal([Greece, Flat, Greece, Greece, Flat], DashboardJson.ActivityGroupNamesOf(body));
            Assert.Equal([scenario.Greece.GroupId, scenario.Flat.GroupId, scenario.Greece.GroupId, scenario.Greece.GroupId, scenario.Flat.GroupId], DashboardJson.ActivityGroupIdsOf(body));
            Assert.Equal([GroupsApp.MemberName, GroupsApp.OwnerName, GroupsApp.MemberName, GroupsApp.OwnerName, GroupsApp.OwnerName], DashboardJson.ActivityActorNamesOf(body));
        }

        [Fact]
        public async Task RecentActivity_OnlyAnswersFiveEvenWhenThereAreMore()
        {
            ActivityScenario scenario = await CreateScenarioAsync();

            JsonElement body = await DashboardRequests.ReadAsync(scenario.Ana);

            Assert.Equal(5, body.GetProperty("recentActivity").GetArrayLength());
        }

        [Fact]
        public async Task RecentActivity_Member_SeesOnlyTheEventsOfHisOwnGroups()
        {
            ActivityScenario scenario = await CreateScenarioAsync();

            JsonElement body = await DashboardRequests.ReadAsync(scenario.Greece.Member);

            Assert.Equal(["ExpenseAdded", "ExpenseAdded", "ExpenseAdded", "GroupCreated"], DashboardJson.ActivityTypesOf(body));
            Assert.All(DashboardJson.ActivityGroupNamesOf(body), groupName => Assert.Equal(Greece, groupName));
        }

        [Fact]
        public async Task RecentActivity_DeletedGroup_LeavesItsEventsOut()
        {
            ActivityScenario scenario = await CreateScenarioAsync();
            await GroupRows.SetDeletedAsync(_app, scenario.Flat.GroupId, scenario.Ana.UserId, 1);

            JsonElement body = await DashboardRequests.ReadAsync(scenario.Ana);

            Assert.Equal(["ExpenseAdded", "ExpenseAdded", "ExpenseAdded", "GroupCreated"], DashboardJson.ActivityTypesOf(body));
            Assert.All(DashboardJson.ActivityGroupNamesOf(body), groupName => Assert.Equal(Greece, groupName));
        }

        [Fact]
        public async Task RecentActivity_GroupTheCallerNoLongerBelongsTo_LeavesItsEventsOut()
        {
            ActivityScenario scenario = await CreateScenarioAsync();
            await GroupRows.EndMembershipAsync(_app, scenario.Greece.GroupId, scenario.Greece.Member.UserId, "Removed");

            JsonElement body = await DashboardRequests.ReadAsync(scenario.Greece.Member);

            Assert.Equal(0, body.GetProperty("recentActivity").GetArrayLength());
        }

        [Fact]
        public async Task RecentActivity_FinishedGroup_StillShowsItsEvents()
        {
            ActivityScenario scenario = await CreateScenarioAsync();
            await DashboardSteps.MarkFinishedAsync(_app, scenario.Greece.GroupId);

            JsonElement body = await DashboardRequests.ReadAsync(scenario.Ana);

            Assert.Equal([Greece, Flat, Greece, Greece, Flat], DashboardJson.ActivityGroupNamesOf(body));
        }

        [Fact]
        public async Task RecentActivity_OtherPeoplesGroups_NeverAppear()
        {
            await CreateScenarioAsync();
            SignedInUser stranger = await _app.SignUpAsync();

            JsonElement body = await DashboardRequests.ReadAsync(stranger);

            Assert.Equal(0, body.GetProperty("recentActivity").GetArrayLength());
        }

        [Fact]
        public async Task RecentActivity_ClosingStarted_IsTheNewestRowWithTheGroupName()
        {
            SignedInUser ana = await _app.SignUpAsync();
            ExpenseGroup group = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, Greece);
            await ClosingSteps.AddCountedExpenseAsync(_app, group.GroupId, ana, group.OwnerRowId);
            _app.Clock.Advance(ClosingSteps.OneMinute);
            await ClosingSteps.StartAsync(ana, group.GroupId);

            JsonElement body = await DashboardRequests.ReadAsync(group.Member);

            Assert.Equal(["ClosingStarted", "GroupCreated"], DashboardJson.ActivityTypesOf(body));
            Assert.Equal([Greece, Greece], DashboardJson.ActivityGroupNamesOf(body));
        }

        private async Task<ActivityScenario> CreateScenarioAsync()
        {
            SignedInUser ana = await _app.SignUpAsync();
            SignedInUser bojan = await _app.SignUpAsync(GroupsApp.MemberName);
            SignedInUser carl = await _app.SignUpAsync("Carl");
            ExpenseGroup greece = await DashboardSteps.CreateGroupAsync(_app, ana, bojan, Greece);
            ExpenseGroup flat = await DashboardSteps.CreateGroupAsync(_app, ana, carl, Flat);
            await DashboardSteps.AddOwnExpenseAsync(_app, greece.GroupId, ana, greece.OwnerRowId, "first");
            await DashboardSteps.AddOwnExpenseAsync(_app, greece.GroupId, bojan, greece.MemberRowId, "second");
            await DashboardSteps.AddOwnExpenseAsync(_app, flat.GroupId, ana, flat.OwnerRowId, "third");
            await DashboardSteps.AddOwnExpenseAsync(_app, greece.GroupId, bojan, greece.MemberRowId, "fourth");

            return new ActivityScenario(ana, greece, flat);
        }
    }
}
