using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Balances;
using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Dashboard;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.GroupSpending;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.GroupPlan
{
    public class GroupPlanSideEffectsTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task DeletedAndRestoredGroup_KeepsItsPlan()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            HttpResponseMessage deleted = await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);
            HttpResponseMessage restored = await GroupRequests.RestoreAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, restored.StatusCode);
            Assert.Equal("60000:EUR", await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
            GroupPlanSteps.AssertPlan(await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId), GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur, 0, 30_000, 0, 0);
            JsonElement body = await AuthRequests.ReadJsonAsync(await GroupRequests.GetAsync(group.Owner.Client, group.GroupId));
            Assert.Equal(GroupPlanSteps.SixHundredEur, body.GetProperty("planAmountMinor").GetInt64());
        }

        [Fact]
        public async Task DeletedGroup_CannotHaveItsPlanChangedUntilItIsRestored()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);
            await GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            HttpResponseMessage whileDeleted = await GroupPlanRequests.DeleteAsync(group.Owner.Client, group.GroupId);
            await GroupRequests.RestoreAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NotFound, whileDeleted.StatusCode);
            Assert.Equal("60000:EUR", await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Balances_WithAPlan_AreTheSameTextAsWithoutOne()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd);
            await GroupPlanSteps.AddAtRateAsync(_app, group, 1_000, GroupPlanRequests.Eur, 61.5610m);
            string without = await BalanceRequests.ReadTextAsync(group.Owner.Client, group.GroupId);

            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, 10_000, GroupPlanRequests.Mkd);
            string withPlan = await BalanceRequests.ReadTextAsync(group.Owner.Client, group.GroupId);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, 10_000, GroupPlanRequests.Eur);
            string withOtherPlan = await BalanceRequests.ReadTextAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(without, withPlan);
            Assert.Equal(without, withOtherPlan);
        }

        [Fact]
        public async Task SettleUp_WithAPlanOverSpent_StillWorksAndKeepsThePlan()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, 10_000, GroupPlanRequests.Mkd);

            Guid settlementId = await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId, 50_000, ExpenseInputs.Mkd);
            await SettlementSteps.ConfirmAsync(group.Owner, group.GroupId, settlementId);

            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
            Assert.Equal("10000:MKD", await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Closing_OfAGroupWithAPlan_StartsAndFinishesAsWithoutOneAndKeepsThePlan()
        {
            SettlementGroup group = await ClosingSteps.CreateGroupWithAnExpenseAsync(_app);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, 50_000, GroupPlanRequests.Mkd);

            await ClosingSteps.StartAsync(group.Owner, group.GroupId);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);
            await ClosingSteps.ConfirmAsync(group.Third, group.GroupId);

            Assert.Equal("Finished", await ClosingSteps.StatusInGroupResponseAsync(group.Owner, group.GroupId));
            Assert.Equal("50000:MKD", await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task OtherEndpoints_AfterAPlanWasSet_GainNoPlanField()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd, categoryId: GroupSpendingSteps.Food);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

            HttpResponseMessage groupList = await GroupRequests.ListAsync(group.Owner.Client);
            HttpResponseMessage members = await GroupRequests.ListMembersAsync(group.Owner.Client, group.GroupId);
            HttpResponseMessage expenseList = await ExpenseRequests.ListAsync(group.Owner.Client, group.GroupId);
            HttpResponseMessage expense = await ExpenseRequests.GetAsync(group.Owner.Client, group.GroupId, expenseId);
            HttpResponseMessage activity = await ExpenseRequests.ActivityAsync(group.Owner.Client, group.GroupId);
            HttpResponseMessage balances = await BalanceRequests.GetAsync(group.Owner.Client, group.GroupId);
            HttpResponseMessage dashboard = await DashboardRequests.GetAsync(group.Owner.Client);

            foreach (HttpResponseMessage response in new[] { groupList, members, expenseList, expense, activity, balances, dashboard })
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                string body = (await AuthRequests.ReadJsonAsync(response)).GetRawText();
                Assert.DoesNotContain("\"plan", body, StringComparison.Ordinal);
            }
        }

        [Fact]
        public async Task Spending_CategoryTotals_AreTheSameWithAndWithoutAPlan()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd, categoryId: GroupSpendingSteps.Food);
            await GroupSpendingSteps.AddAsync(_app, group, 1_000, ExpenseInputs.Eur, GroupSpendingSteps.Transport);
            JsonElement without = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);
            JsonElement withPlan = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(without.GetProperty("currencies").GetRawText(), withPlan.GetProperty("currencies").GetRawText());
            Assert.Equal(JsonValueKind.Null, without.GetProperty("plan").ValueKind);
            Assert.Equal(JsonValueKind.Object, withPlan.GetProperty("plan").ValueKind);
        }
    }
}
