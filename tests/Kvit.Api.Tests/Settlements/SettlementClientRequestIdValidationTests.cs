using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public class SettlementClientRequestIdValidationTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string ClientRequestIdProperty = "clientRequestId";

        private static readonly Dictionary<string, Action<JsonObject>> BrokenClientRequestIds = new()
        {
            ["missing"] = body => body.Remove(ClientRequestIdProperty),
            ["null"] = body => body[ClientRequestIdProperty] = null,
            ["allZeros"] = body => body[ClientRequestIdProperty] = "00000000-0000-0000-0000-000000000000",
            ["emptyText"] = body => body[ClientRequestIdProperty] = string.Empty,
            ["notAGuid"] = body => body[ClientRequestIdProperty] = "not-a-guid",
        };

        public static TheoryData<string> BrokenClientRequestIdNames => [.. BrokenClientRequestIds.Keys];

        [Theory]
        [MemberData(nameof(BrokenClientRequestIdNames))]
        public async Task Record_WithABrokenClientRequestId_Answers400AsAPlainValidationProblemAndWritesNothing(string brokenName)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.PostJsonTextAsync(group.Owner.Client, SettlementRequests.SettlementsPathOf(group.GroupId), RecordJson(group, BrokenClientRequestIds[brokenName]));

            await AssertPlainValidationProblemAsync(response);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(0, await SettlementRows.CountAsync(_app, group.GroupId));
            Assert.Equal(0, await GroupRows.CountUsageEventsAsync(_app, group.Owner.UserId, "SettlementConfirmed"));
        }

        [Fact]
        public async Task Record_WithAValidClientRequestIdInTheSameJsonShape_StillAnswers200AndSavesTheSettlement()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            HttpResponseMessage response = await GroupRequests.PostJsonTextAsync(group.Owner.Client, SettlementRequests.SettlementsPathOf(group.GroupId), RecordJson(group, body => { }));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, await SettlementRows.CountAsync(_app, group.GroupId));
        }

        [Theory]
        [MemberData(nameof(BrokenClientRequestIdNames))]
        public async Task Record_Anonymous_WithABrokenClientRequestId_StillAnswers401(string brokenName)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage response = await GroupRequests.PostJsonTextAsync(anonymous, SettlementRequests.SettlementsPathOf(group.GroupId), RecordJson(group, BrokenClientRequestIds[brokenName]));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private static string RecordJson(SettlementGroup group, Action<JsonObject> change)
        {
            object body = SettlementRequests.RecordBody(group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd, Guid.CreateVersion7());
            JsonObject json = JsonSerializer.SerializeToNode(body, JsonSerializerOptions.Web)!.AsObject();
            change(json);

            return json.ToJsonString();
        }

        private static async Task AssertPlainValidationProblemAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal((int)HttpStatusCode.BadRequest, body.GetProperty("status").GetInt32());
            Assert.False(body.TryGetProperty("errorCode", out _));
        }
    }
}
