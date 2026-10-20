using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.RecurringIncomes
{
    public class RecurringIncomeAccessTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [MemberData(nameof(RecurringIncomeRequests.AllEndpoints), MemberType = typeof(RecurringIncomeRequests))]
        public async Task Anonymous_Answers401(string endpoint)
        {
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage response = await RecurringIncomeRequests.SendAsync(anonymous, endpoint, Guid.CreateVersion7());

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(RecurringIncomeRequests.AllEndpoints), MemberType = typeof(RecurringIncomeRequests))]
        public async Task Guest_Answers403GuestNotAllowedAndWritesNothing(string endpoint)
        {
            GuestMember guest = await RecurringIncomeSteps.JoinAsGuestAsync(_app);
            string before = await RecurringIncomeRows.SnapshotAsync(_app);

            HttpResponseMessage response = await RecurringIncomeRequests.SendAsync(guest.Client, endpoint, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
            Assert.Equal(before, await RecurringIncomeRows.SnapshotAsync(_app));
        }

        [Theory]
        [InlineData("edit")]
        [InlineData("stop")]
        public async Task AnotherUser_ActiveIncomeOfSomeoneElse_Answers404NotFoundAndChangesNothing(string endpoint)
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(owner.Client, RecurringIncomeBody.Monthly());
            string before = await RecurringIncomeRows.SnapshotAsync(_app);

            HttpResponseMessage response = await RecurringIncomeRequests.SendAsync(other.Client, endpoint, recurringIncomeId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.RECURRING_INCOME_NOT_FOUND);
            Assert.Equal(before, await RecurringIncomeRows.SnapshotAsync(_app));
        }

        [Fact]
        public async Task AnotherUser_RestartOfAStoppedIncomeOfSomeoneElse_Answers404NotFoundAndKeepsItStopped()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAndStopAsync(owner.Client, RecurringIncomeBody.Monthly());
            string before = await RecurringIncomeRows.SnapshotAsync(_app);

            HttpResponseMessage response = await RecurringIncomeRequests.RestartAsync(other.Client, recurringIncomeId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.RECURRING_INCOME_NOT_FOUND);
            Assert.Equal(before, await RecurringIncomeRows.SnapshotAsync(_app));
        }

        [Fact]
        public async Task AnotherUser_RestartOfAnActiveIncomeOfSomeoneElse_Answers404NotFoundAndNotNotStopped()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(owner.Client, RecurringIncomeBody.Monthly());

            HttpResponseMessage response = await RecurringIncomeRequests.RestartAsync(other.Client, recurringIncomeId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.RECURRING_INCOME_NOT_FOUND);
        }

        [Fact]
        public async Task AnotherUser_ListNeverShowsTheIncomesOfSomeoneElse()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            await RecurringIncomeSteps.CreateAsync(owner.Client, RecurringIncomeBody.Monthly());
            await RecurringIncomeSteps.CreateAsync(owner.Client, RecurringIncomeBody.Weekly());

            Assert.Empty(await RecurringIncomeSteps.ListRowsAsync(other.Client));
        }
    }
}
