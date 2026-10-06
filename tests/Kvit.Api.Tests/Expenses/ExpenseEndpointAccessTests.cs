using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class ExpenseEndpointAccessTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [MemberData(nameof(ExpenseEndpoints.All), MemberType = typeof(ExpenseEndpoints))]
        public async Task Anonymous_Answers401WhileTheSameRouteExistsForASignedInUser(string method, string suffix)
        {
            SignedInUser user = await _app.SignUpAsync();
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage routed = await ExpenseEndpoints.SendAsync(user.Client, method, suffix, Guid.CreateVersion7(), Guid.CreateVersion7());
            HttpResponseMessage response = await ExpenseEndpoints.SendAsync(anonymous, method, suffix, Guid.CreateVersion7(), Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(routed, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task OneBill_Anonymous_Answers401()
        {
            HttpClient anonymous = _app.CreateClient();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd);

            HttpResponseMessage response = await ExpenseRequests.OneBillAsync(anonymous, input);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(ExpenseEndpoints.All), MemberType = typeof(ExpenseEndpoints))]
        public async Task NonMember_Answers404GroupNotFoundAndWritesNothing(string method, string suffix)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseEndpoints.SendAsync(stranger.Client, method, suffix, group.GroupId, expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }

        [Theory]
        [MemberData(nameof(ExpenseEndpoints.All), MemberType = typeof(ExpenseEndpoints))]
        public async Task RemovedMember_Answers404GroupNotFoundAndWritesNothing(string method, string suffix)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, "Removed");
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseEndpoints.SendAsync(group.Member.Client, method, suffix, group.GroupId, expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }

        [Theory]
        [MemberData(nameof(ExpenseEndpoints.All), MemberType = typeof(ExpenseEndpoints))]
        public async Task DeletedGroup_Answers404GroupNotFoundEvenForTheOwnerAndWritesNothing(string method, string suffix)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));
            await GroupRows.SetDeletedAsync(_app, group.GroupId, group.Owner.UserId, 0);
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseEndpoints.SendAsync(group.Owner.Client, method, suffix, group.GroupId, expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }

        [Theory]
        [MemberData(nameof(ExpenseEndpoints.All), MemberType = typeof(ExpenseEndpoints))]
        public async Task GroupId_ThatIsNotAGuid_Answers404(string method, string suffix)
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await ExpenseEndpoints.SendToSegmentsAsync(user.Client, method, suffix, "not-a-guid", Guid.CreateVersion7().ToString());

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(ExpenseEndpoints.WithExpenseId), MemberType = typeof(ExpenseEndpoints))]
        public async Task ExpenseId_ThatIsNotAGuid_Answers404(string method, string suffix)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            HttpResponseMessage response = await ExpenseEndpoints.SendToSegmentsAsync(group.Owner.Client, method, suffix, group.GroupId.ToString(), "not-a-guid");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeletedSegment_OfTheExpensesRoute_IsTheRecentlyDeletedListAndNotAnExpenseId()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            HttpResponseMessage response = await ExpenseRequests.ListDeletedAsync(group.Member.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task OneBillSegment_OfTheGroupsRoute_IsTheOneBillEndpointAndNotAGroupId()
        {
            SignedInUser user = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd);

            HttpResponseMessage response = await ExpenseRequests.OneBillAsync(user.Client, input);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
