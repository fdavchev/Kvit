using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.Dashboard
{
    public class DashboardAccessTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Get_Anonymous_Answers401WhileASignedInUserGets200()
        {
            SignedInUser user = await _app.SignUpAsync();
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage signedIn = await DashboardRequests.GetAsync(user.Client);
            HttpResponseMessage response = await DashboardRequests.GetAsync(anonymous);

            Assert.Equal(HttpStatusCode.OK, signedIn.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Get_UserInNoGroup_AnswersExactlyTheFiveFieldsAndAllOfThemEmpty()
        {
            SignedInUser user = await _app.SignUpAsync();

            JsonElement body = await DashboardRequests.ReadAsync(user);

            Assert.Equal(DashboardJson.ResponseFieldNames, GroupRequests.PropertyNamesOf(body));
            Assert.Equal(DashboardJson.TotalsFieldNames, GroupRequests.PropertyNamesOf(body.GetProperty("totals")));
            Assert.Empty(DashboardJson.OwedOf(body));
            Assert.Empty(DashboardJson.OweOf(body));
            Assert.Equal(0, body.GetProperty("groups").GetArrayLength());
            Assert.Equal(0, body.GetProperty("needsYou").GetArrayLength());
            Assert.Equal(0, body.GetProperty("recentActivity").GetArrayLength());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("plusTarget").ValueKind);
        }

        [Fact]
        public async Task Get_UserWithAGroup_AnswersExactlyTheFiveFields()
        {
            ExpenseGroup group = await CreateGroupWhereTheOwnerIsOwedAsync();

            JsonElement body = await DashboardRequests.ReadAsync(group.Owner);

            Assert.Equal(DashboardJson.ResponseFieldNames, GroupRequests.PropertyNamesOf(body));
            Assert.Equal(DashboardJson.TotalsFieldNames, GroupRequests.PropertyNamesOf(body.GetProperty("totals")));
        }

        [Fact]
        public async Task Get_AmountInTotals_AnswersExactlyCurrencyAndAmountMinor()
        {
            ExpenseGroup group = await CreateGroupWhereTheOwnerIsOwedAsync();

            JsonElement body = await DashboardRequests.ReadAsync(group.Owner);

            JsonElement owed = Assert.Single(body.GetProperty("totals").GetProperty("owed").EnumerateArray());
            Assert.Equal(DashboardJson.AmountFieldNames, GroupRequests.PropertyNamesOf(owed));
        }

        private async Task<ExpenseGroup> CreateGroupWhereTheOwnerIsOwedAsync()
        {
            SignedInUser owner = await _app.SignUpAsync();
            ExpenseGroup group = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, owner);
            await DashboardSteps.OwnerPaysForBothAsync(_app, group);

            return group;
        }
    }
}
