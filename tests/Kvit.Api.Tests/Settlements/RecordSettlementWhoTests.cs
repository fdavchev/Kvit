using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public class RecordSettlementWhoTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData("Bojan", "Bojan", "Ana", "Pending")]
        [InlineData("Ana", "Bojan", "Ana", "Confirmed")]
        [InlineData("Bojan", "Ana", "Bojan", "Confirmed")]
        [InlineData("Dime", "Dime", "Ana", "Pending")]
        [InlineData("Dime", "Dime", "Marko", "Pending")]
        [InlineData("Bojan", "Bojan", "Marko", "Pending")]
        [InlineData("Dime", "Marko", "Dime", "Confirmed")]
        [InlineData("Ana", "Ana", "Bojan", "Pending")]
        [InlineData("Ana", "Ana", "Marko", "Confirmed")]
        [InlineData("Ana", "Ana", "Cveta", "Confirmed")]
        [InlineData("Ana", "Marko", "Ana", "Confirmed")]
        [InlineData("Ana", "Marko", "Bojan", "Pending")]
        [InlineData("Ana", "Bojan", "Marko", "Confirmed")]
        [InlineData("Ana", "Marko", "Cveta", "Confirmed")]
        public async Task Record_ByWhoIsAllowed_Answers200WithTheStatusOfTheSideTheCallerActsFor(string caller, string from, string to, string expectedStatus)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.UserNamed(caller).Client, group.GroupId, group.RowNamed(from), group.RowNamed(to), 30_000, ExpenseInputs.Mkd);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(expectedStatus, body.GetProperty("status").GetString());
            Assert.Equal(group.UserNamed(caller).UserId, body.GetProperty("recordedByUserId").GetGuid());
            Assert.Equal(expectedStatus, await SettlementRows.ValueAsync(_app, body.GetProperty("id").GetGuid(), "status"));
        }

        [Theory]
        [InlineData("Ana", "Bojan", "Dime")]
        [InlineData("Ana", "Dime", "Bojan")]
        [InlineData("Dime", "Bojan", "Ana")]
        [InlineData("Dime", "Ana", "Bojan")]
        [InlineData("Dime", "Marko", "Bojan")]
        [InlineData("Dime", "Bojan", "Marko")]
        [InlineData("Dime", "Marko", "Cveta")]
        [InlineData("Bojan", "Marko", "Cveta")]
        [InlineData("Bojan", "Dime", "Ana")]
        [InlineData("Bojan", "Ana", "Dime")]
        [InlineData("Bojan", "Marko", "Ana")]
        [InlineData("Bojan", "Ana", "Marko")]
        public async Task Record_ByWhoIsNeitherSideAndCannotActForAPlainName_Answers403SettlementNotAllowedAndWritesNothing(string caller, string from, string to)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementRequests.RecordAsync(group.UserNamed(caller).Client, group.GroupId, group.RowNamed(from), group.RowNamed(to), 30_000, ExpenseInputs.Mkd);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.SETTLEMENT_NOT_ALLOWED);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(0, await SettlementRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Record_ByAPayerWhoIsNotTheOwnerToAPlainNameReceiver_IsPendingBecauseOnlyTheOwnerCanConfirmForAPlainName()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            Guid settlementId = await SettlementSteps.PendingBojanToMarkoAsync(group);

            Assert.Equal("Pending", await SettlementRows.ValueAsync(_app, settlementId, "status"));
        }

        [Fact]
        public async Task Record_ByTheOwnerForAPlainNameReceiver_LeavesTheOwnerAsTheRecorder()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            Guid settlementId = await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.MarkoRowId);

            Assert.Equal(group.Owner.UserId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "recorded_by_user_id"));
            Assert.Equal("Confirmed", await SettlementRows.ValueAsync(_app, settlementId, "status"));
        }
    }
}
