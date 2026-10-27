using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.GroupSpending
{
    public class GetGroupSpendingAccessTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Anonymous_ForAnExistingGroup_Answers401()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd);
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage response = await GroupSpendingRequests.GetAsync(anonymous, group.GroupId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Anonymous_Answers401WhileTheSameRouteExistsForASignedInUser()
        {
            SignedInUser user = await _app.SignUpAsync();
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage routed = await GroupSpendingRequests.GetAsync(user.Client, Guid.CreateVersion7());
            HttpResponseMessage response = await GroupSpendingRequests.GetAsync(anonymous, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(routed, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task EveryCurrentMember_Answers200()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            HttpResponseMessage forOwner = await GroupSpendingRequests.GetAsync(group.Owner.Client, group.GroupId);
            HttpResponseMessage forMember = await GroupSpendingRequests.GetAsync(group.Member.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.OK, forOwner.StatusCode);
            Assert.Equal(HttpStatusCode.OK, forMember.StatusCode);
        }

        [Fact]
        public async Task Guest_AnswersTheSameTotalsAsTheOwner()
        {
            (ExpenseGroup group, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd, categoryId: GroupSpendingSteps.Food);
            await GroupSpendingSteps.AddAsync(_app, group, 1_000, ExpenseInputs.Eur);

            string forOwner = await GroupSpendingRequests.ReadTextAsync(group.Owner.Client, group.GroupId);
            string forGuest = await GroupSpendingRequests.ReadTextAsync(guest.Client, group.GroupId);

            Assert.Equal(forOwner, forGuest);
            Assert.Equal(ExpenseInputs.OneThousandMkd, GroupSpendingJson.TotalOf(await GroupSpendingRequests.ReadAsync(guest.Client, group.GroupId), "MKD"));
        }

        [Fact]
        public async Task Guest_ThatJoinedWithANewName_Answers200()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(owner);
            GuestMember guest = await GuestSteps.JoinWithNewNameAsync(_app, groupId, "Gost");

            HttpResponseMessage response = await GroupSpendingRequests.GetAsync(guest.Client, groupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task NonMember_Answers404GroupNotFound()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd);
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");

            HttpResponseMessage response = await GroupSpendingRequests.GetAsync(stranger.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task MemberOfAnotherGroupOnly_Answers404GroupNotFound()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseGroup other = await _app.CreateExpenseGroupAsync();

            HttpResponseMessage response = await GroupSpendingRequests.GetAsync(other.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task GroupThatDoesNotExist_Answers404GroupNotFound()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await GroupSpendingRequests.GetAsync(user.Client, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task RemovedMember_Answers404GroupNotFound()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, "Removed");

            HttpResponseMessage response = await GroupSpendingRequests.GetAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task MemberWhoLeft_Answers404GroupNotFound()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, "Left");

            HttpResponseMessage response = await GroupSpendingRequests.GetAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task DeletedGroup_Answers404GroupNotFoundEvenForTheOwner()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd);
            await GroupRows.SetDeletedAsync(_app, group.GroupId, group.Owner.UserId, 0);

            HttpResponseMessage response = await GroupSpendingRequests.GetAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task ClosingGroup_Answers200WithTheTotalsToEveryMember()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            string forOwner = await GroupSpendingRequests.ReadTextAsync(group.Owner.Client, group.GroupId);
            string forMember = await GroupSpendingRequests.ReadTextAsync(group.Member.Client, group.GroupId);

            Assert.Equal("Closing", await ClosingSteps.StatusInGroupResponseAsync(group.Owner, group.GroupId));
            Assert.Equal(forOwner, forMember);
            Assert.Equal(ExpenseRows.DefaultAmountMinor, GroupSpendingJson.TotalOf(await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId), "MKD"));
        }

        [Fact]
        public async Task FinishedGroup_Answers200WithTheTotalsToEveryMember()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);

            string forOwner = await GroupSpendingRequests.ReadTextAsync(group.Owner.Client, group.GroupId);
            string forMember = await GroupSpendingRequests.ReadTextAsync(group.Member.Client, group.GroupId);

            Assert.Equal("Finished", await ClosingSteps.StatusInGroupResponseAsync(group.Owner, group.GroupId));
            Assert.Equal(forOwner, forMember);
            Assert.Equal(ExpenseRows.DefaultAmountMinor, GroupSpendingJson.TotalOf(await GroupSpendingRequests.ReadAsync(group.Owner.Client, group.GroupId), "MKD"));
        }

        [Theory]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        public async Task OtherMethods_Answer405AndTheTotalsStayUnchanged(string method)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupSpendingSteps.AddAsync(_app, group, ExpenseInputs.OneThousandMkd, categoryId: GroupSpendingSteps.Food);
            string before = await GroupSpendingRequests.ReadTextAsync(group.Owner.Client, group.GroupId);

            HttpResponseMessage response = await GroupRequests.SendAsync(group.Owner.Client, method, GroupSpendingRequests.PathOf(group.GroupId), new { });

            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
            Assert.Equal(before, await GroupSpendingRequests.ReadTextAsync(group.Owner.Client, group.GroupId));
        }
    }
}
