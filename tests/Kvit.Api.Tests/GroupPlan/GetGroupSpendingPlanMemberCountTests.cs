using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.GroupSpending;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.GroupPlan
{
    public class GetGroupSpendingPlanMemberCountTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        public async Task Get_Plan_AnswersTheNumberOfCurrentMembersAsAWholeNumber(int members)
        {
            ExpenseGroup group = await GroupOfAsync(members);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(JsonValueKind.Number, plan.GetProperty("memberCount").ValueKind);
            Assert.Equal(members, plan.GetProperty("memberCount").GetInt32());
        }

        [Fact]
        public async Task Get_Plan_MemberCountIsTheSameNumberAsTheGroupAnswer()
        {
            ExpenseGroup group = await GroupOfAsync(4);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(await GroupRequests.MemberCountOfAsync(group.Owner.Client, group.GroupId), plan.GetProperty("memberCount").GetInt32());
        }

        [Fact]
        public async Task Get_GroupOfOnlyTheOwner_MemberCountIsOne()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(owner);
            await GroupPlanSteps.SetAsync(owner, groupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            JsonElement plan = await GroupPlanSteps.PlanOfAsync(owner.Client, groupId);

            Assert.Equal(1, plan.GetProperty("memberCount").GetInt32());
        }

        [Fact]
        public async Task Get_Plan_MemberCountIsTheSameForTheOwnerAMemberAndAGuest()
        {
            (ExpenseGroup group, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            JsonElement forOwner = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);
            JsonElement forMember = await GroupPlanSteps.PlanOfAsync(group.Member.Client, group.GroupId);
            JsonElement forGuest = await GroupPlanSteps.PlanOfAsync(guest.Client, group.GroupId);

            Assert.Equal(forOwner.GetProperty("memberCount").GetInt32(), forMember.GetProperty("memberCount").GetInt32());
            Assert.Equal(forOwner.GetProperty("memberCount").GetInt32(), forGuest.GetProperty("memberCount").GetInt32());
        }

        [Fact]
        public async Task Get_MemberLeaves_MemberCountDropsByOne()
        {
            ExpenseGroup group = await GroupOfAsync(3);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            HttpResponseMessage left = await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId);
            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, left.StatusCode);
            Assert.Equal(2, plan.GetProperty("memberCount").GetInt32());
        }

        [Fact]
        public async Task Get_MemberIsRemoved_MemberCountDropsByOne()
        {
            ExpenseGroup group = await GroupOfAsync(3);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            HttpResponseMessage removed = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.PlainRowIds[0]);
            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
            Assert.Equal(2, plan.GetProperty("memberCount").GetInt32());
        }

        [Fact]
        public async Task Get_RemovedMemberIsLetBackIn_MemberCountGrowsAgain()
        {
            ExpenseGroup group = await GroupOfAsync(3);
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);
            await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.PlainRowIds[0]);

            HttpResponseMessage back = await GroupRequests.LetBackInAsync(group.Owner.Client, group.GroupId, group.PlainRowIds[0]);
            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.NoContent, back.StatusCode);
            Assert.Equal(3, plan.GetProperty("memberCount").GetInt32());
        }

        [Fact]
        public async Task Get_PlainNameIsAdded_MemberCountGrowsByOne()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            HttpResponseMessage added = await GroupRequests.AddMemberAsync(group.Owner.Client, group.GroupId, "Marko");
            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.OK, added.StatusCode);
            Assert.Equal(3, plan.GetProperty("memberCount").GetInt32());
        }

        [Fact]
        public async Task Get_GuestWithANewName_CountsAsOneMore()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            await GuestSteps.JoinWithNewNameAsync(_app, group.GroupId, "Gost");
            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(3, plan.GetProperty("memberCount").GetInt32());
        }

        [Fact]
        public async Task Get_GuestWhoTakesAFreeName_DoesNotCountTwice()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);

            await GuestSteps.TapFreeNameAsync(_app, group.GroupId, group.PlainRowIds[0]);
            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(3, plan.GetProperty("memberCount").GetInt32());
        }

        [Fact]
        public async Task Get_ChangingThePlanOrAddingExpenses_DoesNotChangeMemberCount()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.SixHundredEur, GroupPlanRequests.Eur);
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd);

            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);
            JsonElement plan = await GroupPlanSteps.PlanOfAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(3, plan.GetProperty("memberCount").GetInt32());
        }

        private Task<ExpenseGroup> GroupOfAsync(int members)
        {
            string[] plainNames = [.. Enumerable.Range(0, members - 2).Select(index => $"Name{index}")];

            return _app.CreateExpenseGroupAsync(plainNames);
        }
    }
}
