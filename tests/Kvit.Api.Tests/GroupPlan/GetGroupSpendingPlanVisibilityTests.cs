using System.Text.Json;
using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.GroupSpending;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.GroupPlan
{
    public class GetGroupSpendingPlanVisibilityTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Get_Plan_IsTheSameTextForTheOwnerAMemberAndAGuest()
        {
            (ExpenseGroup group, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd);
            await GroupPlanSteps.AddAtRateAsync(_app, group, 1_000, GroupPlanRequests.Eur, 61.5610m);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, 500_000, GroupPlanRequests.Mkd);

            string forOwner = await GroupSpendingRequests.ReadTextAsync(group.Owner.Client, group.GroupId);
            string forMember = await GroupSpendingRequests.ReadTextAsync(group.Member.Client, group.GroupId);
            string forGuest = await GroupSpendingRequests.ReadTextAsync(guest.Client, group.GroupId);

            Assert.Equal(forOwner, forMember);
            Assert.Equal(forOwner, forGuest);
            GroupPlanSteps.AssertPlan(await GroupPlanSteps.PlanOfAsync(guest.Client, group.GroupId), 500_000, GroupPlanRequests.Mkd, 161_600, 166_700, 0, 1_000);
        }

        [Fact]
        public async Task Get_Plan_DoesNotChangeTheCurrenciesPartOfTheAnswer()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd, categoryId: GroupSpendingSteps.Food);
            await GroupPlanSteps.AddAtRateAsync(_app, group, 1_000, GroupPlanRequests.Eur, 61.5610m);
            JsonElement before = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, 500_000, GroupPlanRequests.Mkd);
            JsonElement withMkdPlan = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, 10_000, GroupPlanRequests.Eur);
            JsonElement withEurPlan = await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(before.GetProperty("currencies").GetRawText(), withMkdPlan.GetProperty("currencies").GetRawText());
            Assert.Equal(before.GetProperty("currencies").GetRawText(), withEurPlan.GetProperty("currencies").GetRawText());
            Assert.Equal(GroupSpendingJson.ResponseFieldNames, GroupSpendingJson.PropertyNamesOf(withMkdPlan));
        }

        [Fact]
        public async Task Get_ClosingGroup_StillShowsThePlanToEveryMember()
        {
            SettlementGroup group = await ClosingSteps.CreateGroupWithAnExpenseAsync(_app);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, 500_000, GroupPlanRequests.Mkd);
            await ClosingSteps.StartAsync(group.Owner, group.GroupId);

            JsonElement forOwner = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);
            JsonElement forMember = await GroupPlanSteps.PlanOfAsync(group.Member.Client, group.GroupId);

            Assert.Equal("Closing", await ClosingSteps.StatusInGroupResponseAsync(group.Owner, group.GroupId));
            GroupPlanSteps.AssertPlan(forOwner, 500_000, GroupPlanRequests.Mkd, ExpenseRows.DefaultAmountMinor, 100_000, 0, 0);
            Assert.Equal(forOwner.GetRawText(), forMember.GetRawText());
        }

        [Fact]
        public async Task Get_FinishedGroup_StillShowsThePlanToEveryMember()
        {
            SettlementGroup group = await ClosingSteps.CreateGroupWithAnExpenseAsync(_app);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, 50_000, GroupPlanRequests.Mkd);
            await ClosingSteps.StartAsync(group.Owner, group.GroupId);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);
            await ClosingSteps.ConfirmAsync(group.Third, group.GroupId);

            JsonElement forOwner = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);
            JsonElement forThird = await GroupPlanSteps.PlanOfAsync(group.Third.Client, group.GroupId);

            Assert.Equal("Finished", await ClosingSteps.StatusInGroupResponseAsync(group.Owner, group.GroupId));
            GroupPlanSteps.AssertPlan(forOwner, 50_000, GroupPlanRequests.Mkd, ExpenseRows.DefaultAmountMinor, 10_000, 50_000, 0);
            Assert.Equal(forOwner.GetRawText(), forThird.GetRawText());
        }

        [Fact]
        public async Task Get_ReadingThePlan_ChangesNothingInTheGroupItsEventsOrTheUsageLog()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, 500_000, GroupPlanRequests.Mkd);
            string groupBefore = await GroupRows.SnapshotAsync(_app, group.GroupId);
            string usageBefore = await GroupPlanSteps.UsageSnapshotAsync(_app, group.Owner, group.Member);

            await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId);
            await GroupSpendingRequests.ReadAsync(group.Member.Client, group.GroupId);

            Assert.Equal(groupBefore, await GroupRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(usageBefore, await GroupPlanSteps.UsageSnapshotAsync(_app, group.Owner, group.Member));
        }
    }
}
