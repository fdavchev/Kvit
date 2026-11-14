using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Xunit;

namespace Kvit.Api.Tests.Export
{
    public class ExportActivityTests(ExportApp _app) : IClassFixture<ExportApp>
    {
        [Fact]
        public async Task Export_Activity_IsTheGroupsFeedOldestFirstWithTheSameFields()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();
            JsonElement feed = await AuthRequests.ReadJsonAsync(await ExpenseRequests.ActivityAsync(scenario.Exporter.Client, scenario.GroupId));

            List<JsonElement> feedOldestFirst = [.. feed.GetProperty("events").EnumerateArray().Reverse()];
            List<JsonElement> exported = ActivityOf(body, scenario.GroupId);

            Assert.NotEmpty(exported);
            Assert.Equal(ExportBody.IdsOf(feedOldestFirst), ExportBody.IdsOf(exported));
            for (int index = 0; index < exported.Count; index++)
            {
                foreach (string fieldName in ExportRequests.ActivityEventFieldNames)
                {
                    Assert.Equal(feedOldestFirst[index].GetProperty(fieldName).GetRawText(), exported[index].GetProperty(fieldName).GetRawText());
                }
            }
        }

        [Fact]
        public async Task Export_Activity_ExpenseAddedRow_ShowsWhenWhoAndTheExpenseFacts()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement row = Assert.Single(
                ActivityOf(body, scenario.GroupId),
                candidate => candidate.GetProperty("type").GetString() == "ExpenseAdded" && ExportBody.GuidOrNull(candidate, "expenseId") == scenario.DinnerExpenseId);

            Assert.Equal(scenario.Exporter.Form.DisplayName, row.GetProperty("actorName").GetString());
            Assert.Equal(ExpensesApp.ClockStart, row.GetProperty("createdAt").GetDateTimeOffset());
            JsonElement data = row.GetProperty("data");
            Assert.Equal(ExportScenarioSteps.DinnerTitle, data.GetProperty("title").GetString());
            Assert.Equal(120_000, data.GetProperty("amountMinor").GetInt64());
            Assert.Equal("MKD", data.GetProperty("currency").GetString());
            Assert.Equal(JsonValueKind.Null, row.GetProperty("changes").ValueKind);
        }

        [Fact]
        public async Task Export_Activity_ShowsTheOtherPersonByNameAndNeverByEmail()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            List<JsonElement> recordedByOther = [.. ActivityOf(body, scenario.GroupId)
                .Where(row => row.GetProperty("type").GetString() == "SettlementRecorded"
                    && row.GetProperty("actorName").GetString() == ExportScenarioSteps.OtherDisplayName)];

            Assert.Equal(2, recordedByOther.Count);
            Assert.All(recordedByOther, row => Assert.Equal(ExportScenarioSteps.OtherDisplayName, row.GetProperty("data").GetProperty("fromName").GetString()));
            body.AssertDoesNotMention(scenario.Other.Form.Email);
        }

        [Fact]
        public async Task Export_Activity_OfGroupsThePersonIsNotIn_IsNotListed()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            body.AssertDoesNotMention(scenario.StrangersEventId.ToString());
            body.AssertDoesNotMention(scenario.DeletedGroupEventId.ToString());
            body.AssertDoesNotMention(ExportScenarioSteps.StrangersGroupName);
        }

        [Fact]
        public async Task Export_Activity_OfTheOtherMember_ListsTheRowOfTheGroupOnlyThatPersonIsIn()
        {
            ExportScenario scenario = await _app.ScenarioAsync();

            ExportBody body = await ExportRequests.GetBodyAsync(scenario.Other.Client);

            JsonElement row = Assert.Single(ActivityOf(body, scenario.StrangersGroupId));
            Assert.Equal(scenario.StrangersEventId, row.GetProperty("id").GetGuid());
            Assert.Equal(ExportScenarioSteps.OtherDisplayName, row.GetProperty("actorName").GetString());
        }

        [Fact]
        public async Task Export_Activity_OfTheLeftGroup_IsOnlyTheExportersOwnLeaving()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement row = Assert.Single(ActivityOf(body, scenario.LeftGroupId));

            Assert.Equal("MemberLeft", row.GetProperty("type").GetString());
            Assert.Equal(scenario.Exporter.Form.DisplayName, row.GetProperty("actorName").GetString());
            Assert.Equal(scenario.Exporter.Form.DisplayName, row.GetProperty("data").GetProperty("name").GetString());
            Assert.Equal(ExpensesApp.ClockStart, row.GetProperty("createdAt").GetDateTimeOffset());
        }

        private static List<JsonElement> ActivityOf(ExportBody body, Guid groupId)
        {
            return ExportBody.ListOf(body.GroupWithId(groupId), "activity");
        }
    }
}
