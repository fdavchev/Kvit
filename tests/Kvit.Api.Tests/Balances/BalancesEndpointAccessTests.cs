using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Balances
{
    public class BalancesEndpointAccessTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Anonymous_Answers401WhileTheSameRouteExistsForASignedInUser()
        {
            SignedInUser user = await _app.SignUpAsync();
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage routed = await BalanceRequests.GetAsync(user.Client, Guid.CreateVersion7());
            HttpResponseMessage response = await BalanceRequests.GetAsync(anonymous, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(routed, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Anonymous_ForAnExistingGroup_Answers401()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await BalanceScenario.AddMixedExpensesAsync(_app, group);
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage response = await BalanceRequests.GetAsync(anonymous, group.GroupId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task NonMember_Answers404GroupNotFound()
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await BalanceScenario.AddMixedExpensesAsync(_app, group);
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");

            HttpResponseMessage response = await BalanceRequests.GetAsync(stranger.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task GroupThatDoesNotExist_Answers404GroupNotFound()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await BalanceRequests.GetAsync(user.Client, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task RemovedMember_Answers404GroupNotFound()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await _app.AddExpenseAsync(group.Member, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId));
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, "Removed");

            HttpResponseMessage response = await BalanceRequests.GetAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task MemberWhoLeft_Answers404GroupNotFound()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await _app.AddExpenseAsync(group.Member, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId));
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, "Left");

            HttpResponseMessage response = await BalanceRequests.GetAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task DeletedGroup_Answers404GroupNotFoundEvenForTheOwner()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId));
            await GroupRows.SetDeletedAsync(_app, group.GroupId, group.Owner.UserId, 0);

            HttpResponseMessage response = await BalanceRequests.GetAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Fact]
        public async Task GroupId_ThatIsNotAGuid_Answers404()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await BalanceRequests.GetSegmentAsync(user.Client, "not-a-guid");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task EveryCurrentMember_Answers200()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            HttpResponseMessage forOwner = await BalanceRequests.GetAsync(group.Owner.Client, group.GroupId);
            HttpResponseMessage forMember = await BalanceRequests.GetAsync(group.Member.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.OK, forOwner.StatusCode);
            Assert.Equal(HttpStatusCode.OK, forMember.StatusCode);
        }

        [Theory]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        public async Task OtherMethods_Answer405AndTheBalancesStayUnchanged(string method)
        {
            ExpenseGroup group = await BalanceScenario.CreateGroupAsync(_app);
            await BalanceScenario.AddMixedExpensesAsync(_app, group);
            string before = await BalanceRequests.ReadTextAsync(group.Owner.Client, group.GroupId);

            HttpResponseMessage response = await GroupRequests.SendAsync(group.Owner.Client, method, BalanceRequests.PathOf(group.GroupId), new { });

            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
            Assert.Equal(before, await BalanceRequests.ReadTextAsync(group.Owner.Client, group.GroupId));
        }
    }
}
