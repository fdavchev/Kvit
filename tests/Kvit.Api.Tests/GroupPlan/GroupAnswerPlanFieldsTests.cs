using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.GroupPlan
{
    public class GroupAnswerPlanFieldsTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Get_GroupWithoutAPlan_AnswersBothPlanFieldsPresentAndNull()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            JsonElement body = await ReadGroupAsync(group.Owner, group.GroupId);

            Assert.True(body.TryGetProperty("planAmountMinor", out JsonElement amount));
            Assert.True(body.TryGetProperty("planCurrency", out JsonElement currency));
            Assert.Equal(JsonValueKind.Null, amount.ValueKind);
            Assert.Equal(JsonValueKind.Null, currency.ValueKind);
        }

        [Fact]
        public async Task Get_GroupWithAPlan_AnswersTheAmountAsANumberAndTheCurrencyAsText()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            JsonElement body = await ReadGroupAsync(group.Owner, group.GroupId);

            Assert.Equal(JsonValueKind.Number, body.GetProperty("planAmountMinor").ValueKind);
            Assert.Equal(GroupPlanSteps.SixHundredEur, body.GetProperty("planAmountMinor").GetInt64());
            Assert.Equal(JsonValueKind.String, body.GetProperty("planCurrency").ValueKind);
            Assert.Equal(GroupPlanRequests.Eur, body.GetProperty("planCurrency").GetString());
        }

        [Fact]
        public async Task Get_PlanStoredInTheRow_IsTheOneTheGroupAnswerShows()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanRows.SetAsync(_app, group.GroupId, 750_000, GroupPlanRequests.Mkd);

            JsonElement body = await ReadGroupAsync(group.Owner, group.GroupId);

            Assert.Equal(750_000, body.GetProperty("planAmountMinor").GetInt64());
            Assert.Equal(GroupPlanRequests.Mkd, body.GetProperty("planCurrency").GetString());
        }

        [Fact]
        public async Task Get_AfterThePlanWasChangedAndThenRemoved_FollowsEachChange()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);
            JsonElement changed = await ReadGroupAsync(group.Owner, group.GroupId);

            await GroupPlanSteps.RemoveAsync(group.Owner, group.GroupId);
            JsonElement removed = await ReadGroupAsync(group.Owner, group.GroupId);

            Assert.Equal(GroupPlanSteps.SixHundredEur, changed.GetProperty("planAmountMinor").GetInt64());
            Assert.Equal(GroupPlanRequests.Eur, changed.GetProperty("planCurrency").GetString());
            Assert.Equal(JsonValueKind.Null, removed.GetProperty("planAmountMinor").ValueKind);
            Assert.Equal(JsonValueKind.Null, removed.GetProperty("planCurrency").ValueKind);
        }

        [Fact]
        public async Task Create_NewGroup_AnswersBothPlanFieldsPresentAndNull()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await GroupRequests.CreateAsync(user.Client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(GroupRequests.GroupFieldNames, GroupRequests.PropertyNamesOf(body));
            Assert.Equal(JsonValueKind.Null, body.GetProperty("planAmountMinor").ValueKind);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("planCurrency").ValueKind);
        }

        [Fact]
        public async Task Get_EveryMemberAndAGuest_SeeTheSamePlanFieldsAsTheOwner()
        {
            (ExpenseGroup group, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            JsonElement forMember = await AuthRequests.ReadJsonAsync(await GroupRequests.GetAsync(group.Member.Client, group.GroupId));
            JsonElement forGuest = await AuthRequests.ReadJsonAsync(await GroupRequests.GetAsync(guest.Client, group.GroupId));

            Assert.Equal(GroupRequests.GroupFieldNames, GroupRequests.PropertyNamesOf(forGuest));
            foreach (JsonElement body in new[] { forMember, forGuest })
            {
                Assert.Equal(GroupPlanSteps.SixHundredEur, body.GetProperty("planAmountMinor").GetInt64());
                Assert.Equal(GroupPlanRequests.Eur, body.GetProperty("planCurrency").GetString());
            }
        }

        [Fact]
        public async Task Get_ClosingGroup_StillAnswersThePlanFields()
        {
            SettlementGroup group = await ClosingSteps.CreateGroupWithAnExpenseAsync(_app);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);
            await ClosingSteps.StartAsync(group.Owner, group.GroupId);

            JsonElement body = await ClosingSteps.ReadGroupAsync(group.Member, group.GroupId);

            Assert.Equal("Closing", body.GetProperty("status").GetString());
            Assert.Equal(GroupPlanSteps.TwoThousandMkd, body.GetProperty("planAmountMinor").GetInt64());
            Assert.Equal(GroupPlanRequests.Mkd, body.GetProperty("planCurrency").GetString());
        }

        [Fact]
        public async Task Get_FinishedGroup_StillAnswersThePlanFields()
        {
            SettlementGroup group = await ClosingSteps.CreateGroupWithAnExpenseAsync(_app);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);
            await ClosingSteps.StartAsync(group.Owner, group.GroupId);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);
            await ClosingSteps.ConfirmAsync(group.Third, group.GroupId);

            JsonElement body = await ClosingSteps.ReadGroupAsync(group.Third, group.GroupId);

            Assert.Equal("Finished", body.GetProperty("status").GetString());
            Assert.Equal(GroupPlanSteps.TwoThousandMkd, body.GetProperty("planAmountMinor").GetInt64());
            Assert.Equal(GroupPlanRequests.Mkd, body.GetProperty("planCurrency").GetString());
        }

        [Fact]
        public async Task Get_OneBillGroup_AnswersThePlanFields()
        {
            SignedInUser owner = await _app.SignUpAsync();
            (Guid groupId, _) = await _app.CreateOneBillAsync(owner, OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko"));
            await GroupPlanSteps.SetAsync(owner, groupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

            JsonElement body = await ReadGroupAsync(owner, groupId);

            Assert.Equal("OneBill", body.GetProperty("kind").GetString());
            Assert.Equal(GroupRequests.GroupFieldNames, GroupRequests.PropertyNamesOf(body));
            Assert.Equal(GroupPlanSteps.TwoThousandMkd, body.GetProperty("planAmountMinor").GetInt64());
        }

        [Fact]
        public async Task GroupList_StaysWithoutPlanFields()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

            HttpResponseMessage response = await GroupRequests.ListAsync(group.Owner.Client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string text = (await AuthRequests.ReadJsonAsync(response)).GetRawText();
            Assert.DoesNotContain("\"plan", text, StringComparison.Ordinal);
        }

        private static async Task<JsonElement> ReadGroupAsync(SignedInUser viewer, Guid groupId)
        {
            HttpResponseMessage response = await GroupRequests.GetAsync(viewer.Client, groupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await AuthRequests.ReadJsonAsync(response);
        }
    }
}
