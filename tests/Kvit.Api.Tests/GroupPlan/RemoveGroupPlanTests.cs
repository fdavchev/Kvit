using System.Net;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.GroupPlan
{
    public class RemoveGroupPlanTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Delete_OwnerOfAGroupWithAPlan_Answers204WithNoBodyAndClearsBothColumns()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            HttpResponseMessage response = await GroupPlanRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Null(await GroupRows.GroupValueAsync(_app, group.GroupId, "plan_amount_minor"));
            Assert.Null(await GroupRows.GroupValueAsync(_app, group.GroupId, "plan_currency"));
        }

        [Fact]
        public async Task Delete_GroupWithoutAPlan_Answers204AndChangesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            string before = await GroupRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupPlanRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Delete_TwiceInARow_AnswersTwo204AndLeavesNoPlan()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

            HttpResponseMessage first = await GroupPlanRequests.DeleteAsync(group.Owner.Client, group.GroupId);
            HttpResponseMessage second = await GroupPlanRequests.DeleteAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
            Assert.Equal(GroupPlanRows.NoPlan, await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Delete_PlanOfOneGroup_NeverTouchesThePlanOfAnotherGroup()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseGroup other = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);
            await GroupPlanSteps.SetAsync(other.Owner, other.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            await GroupPlanSteps.RemoveAsync(group.Owner, group.GroupId);

            Assert.Equal(GroupPlanRows.NoPlan, await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
            Assert.Equal("60000:EUR", await GroupPlanRows.PlanOfAsync(_app, other.GroupId));
        }

        [Fact]
        public async Task Delete_ChangesNoOtherSettingOfTheGroup()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

            await GroupPlanSteps.RemoveAsync(group.Owner, group.GroupId);

            Assert.Equal(GroupRows.DefaultName, await GroupRows.GroupValueAsync(_app, group.GroupId, "name"));
            Assert.Equal(GroupRows.DefaultEmoji, await GroupRows.GroupValueAsync(_app, group.GroupId, "emoji"));
            Assert.Equal(GroupRows.DefaultCurrency, await GroupRows.GroupValueAsync(_app, group.GroupId, "default_currency"));
            Assert.Equal("Open", await GroupRows.GroupValueAsync(_app, group.GroupId, "status"));
        }
    }
}
