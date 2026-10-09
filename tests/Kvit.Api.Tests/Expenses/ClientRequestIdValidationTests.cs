using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class ClientRequestIdValidationTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
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
        public async Task Add_WithABrokenClientRequestId_Answers400AsAPlainValidationProblemAndWritesNothing(string brokenName)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.PostJsonTextAsync(group.Owner.Client, ExpenseRequests.ExpensesPathOf(group.GroupId), AddJson(input, BrokenClientRequestIds[brokenName]));

            await AssertPlainValidationProblemAsync(response);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(0, await GroupRows.CountUsageEventsAsync(_app, group.Owner.UserId, "ExpenseAdded"));
        }

        [Theory]
        [MemberData(nameof(BrokenClientRequestIdNames))]
        public async Task OneBill_WithABrokenClientRequestId_Answers400AsAPlainValidationProblemAndWritesNoGroupAndNoExpense(string brokenName)
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            HttpResponseMessage response = await GroupRequests.PostJsonTextAsync(caller.Client, ExpenseRequests.OneBillPath, OneBillJson(input, BrokenClientRequestIds[brokenName]));

            await AssertPlainValidationProblemAsync(response);
            await _app.AssertNothingWrittenForAsync(caller);
        }

        [Fact]
        public async Task Add_WithAValidClientRequestIdInTheSameJsonShape_StillAnswers200AndSavesTheExpense()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId);

            HttpResponseMessage response = await GroupRequests.PostJsonTextAsync(group.Owner.Client, ExpenseRequests.ExpensesPathOf(group.GroupId), AddJson(input, body => { }));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, await ExpenseRows.CountExpensesAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task OneBill_WithAValidClientRequestIdInTheSameJsonShape_StillAnswers200AndSavesTheBill()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            HttpResponseMessage response = await GroupRequests.PostJsonTextAsync(caller.Client, ExpenseRequests.OneBillPath, OneBillJson(input, body => { }));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, await GroupRows.CountGroupsOwnedByAsync(_app, caller.UserId));
            Assert.Equal(1, await ExpenseRows.CountExpensesCreatedByAsync(_app, caller.UserId));
        }

        [Theory]
        [MemberData(nameof(BrokenClientRequestIdNames))]
        public async Task Add_Anonymous_WithABrokenClientRequestId_StillAnswers401(string brokenName)
        {
            HttpClient anonymous = _app.CreateClient();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, Guid.CreateVersion7(), Guid.CreateVersion7());

            HttpResponseMessage response = await GroupRequests.PostJsonTextAsync(anonymous, ExpenseRequests.ExpensesPathOf(Guid.CreateVersion7()), AddJson(input, BrokenClientRequestIds[brokenName]));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(BrokenClientRequestIdNames))]
        public async Task OneBill_Anonymous_WithABrokenClientRequestId_StillAnswers401(string brokenName)
        {
            HttpClient anonymous = _app.CreateClient();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            HttpResponseMessage response = await GroupRequests.PostJsonTextAsync(anonymous, ExpenseRequests.OneBillPath, OneBillJson(input, BrokenClientRequestIds[brokenName]));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private static string AddJson(ExpenseInput input, Action<JsonObject> change)
        {
            return JsonTextOf(ExpenseRequests.AddBody(input, Guid.CreateVersion7()), change);
        }

        private static string OneBillJson(OneBillInput input, Action<JsonObject> change)
        {
            return JsonTextOf(ExpenseRequests.OneBillBody(input, Guid.CreateVersion7()), change);
        }

        private static string JsonTextOf(object body, Action<JsonObject> change)
        {
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
