using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.GroupPlan
{
    public class SetGroupPlanTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Put_OwnerOfAGroupWithoutAPlan_Answers204WithNoBodyAndStoresTheAmountAndCurrency()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            HttpResponseMessage response = await GroupPlanRequests.SetAsync(group.Owner.Client, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Equal("200000:MKD", await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Put_PlanInEuros_StoresTheCurrencyAsTheEnumName()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            HttpResponseMessage response = await GroupPlanRequests.SetAsync(group.Owner.Client, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("60000", await GroupRows.GroupValueAsync(_app, group.GroupId, "plan_amount_minor"));
            Assert.Equal("EUR", await GroupRows.GroupValueAsync(_app, group.GroupId, "plan_currency"));
        }

        [Fact]
        public async Task Put_PlanInTheOtherCurrencyThanTheGroupDefault_IsStoredAsGiven()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = (await AuthRequests.ReadJsonAsync(await GroupRequests.CreateAsync(owner.Client, currency: GroupPlanRequests.Eur))).GetProperty("id").GetGuid();

            await GroupPlanSteps.SetAsync(owner, groupId, 5_000_000, GroupPlanRequests.Mkd);

            Assert.Equal("EUR", await GroupRows.GroupValueAsync(_app, groupId, "default_currency"));
            Assert.Equal("5000000:MKD", await GroupPlanRows.PlanOfAsync(_app, groupId));
        }

        [Fact]
        public async Task Put_SecondPlan_ReplacesTheAmountAndTheCurrencyTogether()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

            HttpResponseMessage response = await GroupPlanRequests.SetAsync(group.Owner.Client, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("60000:EUR", await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Put_OnlyTheAmountChanges_KeepsTheCurrency()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, 70_000, GroupPlanRequests.Eur);

            Assert.Equal("70000:EUR", await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Put_OnlyTheCurrencyChanges_KeepsTheAmountNumber()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, 100_000, GroupPlanRequests.Eur);

            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, 100_000, GroupPlanRequests.Mkd);

            Assert.Equal("100000:MKD", await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Put_TheSameValueAgain_Answers204AndChangesNothingInTheGroupRowItsMembersOrItsEvents()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);
            string before = await GroupRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupPlanRequests.SetAsync(group.Owner.Client, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Put_AfterTheRemovalOfAPlan_StoresTheNewPlan()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);
            await GroupPlanSteps.RemoveAsync(group.Owner, group.GroupId);

            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            Assert.Equal("60000:EUR", await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Put_ChangesNoOtherSettingOfTheGroup()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            Assert.Equal(GroupRows.DefaultName, await GroupRows.GroupValueAsync(_app, group.GroupId, "name"));
            Assert.Equal(GroupRows.DefaultEmoji, await GroupRows.GroupValueAsync(_app, group.GroupId, "emoji"));
            Assert.Equal(GroupRows.DefaultCurrency, await GroupRows.GroupValueAsync(_app, group.GroupId, "default_currency"));
            Assert.Equal("Open", await GroupRows.GroupValueAsync(_app, group.GroupId, "status"));
        }

        [Fact]
        public async Task Put_ForOneGroup_NeverTouchesThePlanOfAnotherGroup()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseGroup other = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(other.Owner, other.GroupId, 300_000, GroupPlanRequests.Mkd);

            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            Assert.Equal("300000:MKD", await GroupPlanRows.PlanOfAsync(_app, other.GroupId));
            Assert.Equal("60000:EUR", await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Put_OneBillGroup_StoresThePlanUnderTheSameRules()
        {
            SignedInUser owner = await _app.SignUpAsync();
            (Guid groupId, _) = await _app.CreateOneBillAsync(owner, OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko"));

            HttpResponseMessage response = await GroupPlanRequests.SetAsync(owner.Client, groupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

            Assert.Equal("OneBill", await GroupRows.GroupValueAsync(_app, groupId, "kind"));
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("200000:MKD", await GroupPlanRows.PlanOfAsync(_app, groupId));
        }

        [Fact]
        public async Task Put_OneBillGroupWithAnInvalidAmount_Answers400LikeAnyGroup()
        {
            SignedInUser owner = await _app.SignUpAsync();
            (Guid groupId, _) = await _app.CreateOneBillAsync(owner, OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko"));

            HttpResponseMessage response = await GroupPlanRequests.SetAsync(owner.Client, groupId, 150, GroupPlanRequests.Mkd);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, GroupPlanRequests.AmountInvalidCode);
            Assert.Equal(GroupPlanRows.NoPlan, await GroupPlanRows.PlanOfAsync(_app, groupId));
        }

        [Fact]
        public async Task Put_ByTheNewOwnerAfterTheOwnershipMoved_Answers204AndTheOldOwnerIsRefused()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);
            HttpResponseMessage moved = await GroupRequests.MakeOwnerAsync(group.Owner.Client, group.GroupId, group.MemberRowId);
            Assert.Equal(HttpStatusCode.NoContent, moved.StatusCode);

            HttpResponseMessage byNewOwner = await GroupPlanRequests.SetAsync(group.Member.Client, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);
            HttpResponseMessage byOldOwner = await GroupPlanRequests.SetAsync(group.Owner.Client, group.GroupId, 1_000, GroupPlanRequests.Eur);

            Assert.Equal(HttpStatusCode.NoContent, byNewOwner.StatusCode);
            await ProblemResponse.AssertAsync(byOldOwner, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal("60000:EUR", await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }
    }
}
