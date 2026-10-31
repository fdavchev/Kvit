using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.GroupPlan
{
    public class GetGroupSpendingPlanPerPersonTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData(60_000, "EUR", 4, 15_000)]
        [InlineData(100_000, "MKD", 3, 33_300)]
        [InlineData(100_000, "MKD", 7, 14_300)]
        [InlineData(200_000, "MKD", 2, 100_000)]
        public async Task Get_Plan_IsSharedByTheCurrentMembersAndRoundedToTheNearestStep(long amountMinor, string currency, int members, long expectedPerPerson)
        {
            ExpenseGroup group = await GroupOfAsync(members);
            await SetPlanDirectlyAsync(group, amountMinor, currency);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(expectedPerPerson, plan.GetProperty("perPersonMinor").GetInt64());
        }

        [Theory]
        [InlineData(5, 2, 3)]
        [InlineData(7, 2, 4)]
        [InlineData(1, 2, 1)]
        [InlineData(3, 2, 2)]
        [InlineData(1, 3, 0)]
        [InlineData(2, 3, 1)]
        [InlineData(4, 3, 1)]
        [InlineData(5, 4, 1)]
        [InlineData(6, 4, 2)]
        public async Task Get_EurPlan_RoundsExactHalvesOfACentAwayFromZero(long amountCents, int members, long expectedPerPerson)
        {
            ExpenseGroup group = await GroupOfAsync(members);
            await SetPlanDirectlyAsync(group, amountCents, GroupPlanRequests.Eur);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(expectedPerPerson, plan.GetProperty("perPersonMinor").GetInt64());
        }

        [Theory]
        [InlineData(300, 2, 200)]
        [InlineData(100, 2, 100)]
        [InlineData(500, 2, 300)]
        [InlineData(1_000, 4, 300)]
        [InlineData(100, 3, 0)]
        [InlineData(200, 3, 100)]
        [InlineData(1_500, 3, 500)]
        [InlineData(1_400, 4, 400)]
        public async Task Get_MkdPlan_RoundsToTheNearestWholeDenarWithExactHalvesAwayFromZero(long amountDeni, int members, long expectedPerPerson)
        {
            ExpenseGroup group = await GroupOfAsync(members);
            await SetPlanDirectlyAsync(group, amountDeni, GroupPlanRequests.Mkd);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(expectedPerPerson, plan.GetProperty("perPersonMinor").GetInt64());
        }

        [Fact]
        public async Task Get_EurPlanAtTheBound_RoundsTheHalfCentAwayFromZeroExactly()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await SetPlanDirectlyAsync(group, GroupPlanRequests.LargestEurAmount, GroupPlanRequests.Eur);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(500_000_000_000, plan.GetProperty("perPersonMinor").GetInt64());
        }

        [Fact]
        public async Task Get_MkdPlanAtTheBound_DividesExactlyBetweenThreeMembers()
        {
            ExpenseGroup group = await GroupOfAsync(3);
            await SetPlanDirectlyAsync(group, GroupPlanRequests.LargestMkdAmount, GroupPlanRequests.Mkd);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(333_333_333_300, plan.GetProperty("perPersonMinor").GetInt64());
        }

        [Fact]
        public async Task Get_MemberLeaves_PerPersonGrows()
        {
            ExpenseGroup group = await GroupOfAsync(3);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);
            JsonElement before = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            HttpResponseMessage left = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);
            JsonElement after = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, left.StatusCode);
            Assert.Equal(20_000, before.GetProperty("perPersonMinor").GetInt64());
            Assert.Equal(30_000, after.GetProperty("perPersonMinor").GetInt64());
        }

        [Fact]
        public async Task Get_MemberIsRemoved_PerPersonGrows()
        {
            ExpenseGroup group = await GroupOfAsync(3);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            HttpResponseMessage removed = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.PlainRowIds[0]);
            JsonElement after = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
            Assert.Equal(30_000, after.GetProperty("perPersonMinor").GetInt64());
        }

        [Fact]
        public async Task Get_RemovedMemberIsLetBackIn_PerPersonShrinksAgain()
        {
            ExpenseGroup group = await GroupOfAsync(3);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);
            await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.PlainRowIds[0]);

            HttpResponseMessage back = await GroupRequests.LetBackInAsync(group.Owner.Client, group.GroupId, group.PlainRowIds[0]);
            JsonElement after = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, back.StatusCode);
            Assert.Equal(20_000, after.GetProperty("perPersonMinor").GetInt64());
        }

        [Fact]
        public async Task Get_PlainNameIsAdded_PerPersonShrinks()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);
            JsonElement before = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            HttpResponseMessage added = await GroupRequests.AddMemberAsync(group.Owner.Client, group.GroupId, "Marko");
            JsonElement after = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.OK, added.StatusCode);
            Assert.Equal(30_000, before.GetProperty("perPersonMinor").GetInt64());
            Assert.Equal(20_000, after.GetProperty("perPersonMinor").GetInt64());
        }

        [Fact]
        public async Task Get_GuestWithANewName_CountsAsOneMore()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            await GuestSteps.JoinWithNewNameAsync(_app, group.GroupId, "Gost");
            JsonElement after = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(20_000, after.GetProperty("perPersonMinor").GetInt64());
        }

        [Fact]
        public async Task Get_GuestWhoTakesAFreeName_DoesNotCountTwice()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);
            JsonElement before = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            await GuestSteps.TapFreeNameAsync(_app, group.GroupId, group.PlainRowIds[0]);
            JsonElement after = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(20_000, before.GetProperty("perPersonMinor").GetInt64());
            Assert.Equal(20_000, after.GetProperty("perPersonMinor").GetInt64());
        }

        [Fact]
        public async Task Get_GroupOfOnlyTheOwner_GivesThePersonTheWholePlan()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(owner);
            await GroupPlanSteps.SetAsync(owner, groupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(owner.Client, groupId);

            Assert.Equal(GroupPlanSteps.SixHundredEur, plan.GetProperty("perPersonMinor").GetInt64());
        }

        [Fact]
        public async Task Get_ExpensesInTheGroup_DoNotChangeThePerPersonAmount()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);
            JsonElement before = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            await GroupPlanSteps.AddAtRateAsync(_app, group, 5_000, GroupPlanRequests.Eur, 61.5610m);
            JsonElement after = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(before.GetProperty("perPersonMinor").GetInt64(), after.GetProperty("perPersonMinor").GetInt64());
        }

        private Task<ExpenseGroup> GroupOfAsync(int members)
        {
            string[] plainNames = [.. Enumerable.Range(0, members - 2).Select(index => $"Name{index}")];

            return _app.CreateExpenseGroupAsync(plainNames);
        }

        private Task SetPlanDirectlyAsync(ExpenseGroup group, long amountMinor, string currency)
        {
            return GroupPlanRows.SetAsync(_app, group.GroupId, amountMinor, currency);
        }
    }
}
