using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.RecurringIncomes
{
    public class RecurringIncomeLimitTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const int Limit = RecurringIncomeRequests.ActiveLimit;

        [Fact]
        public async Task Create_TwentyActiveIncomes_AreAllSaved()
        {
            SignedInUser user = await _app.SignUpAsync();

            await RecurringIncomeSteps.CreateManyAsync(user.Client, Limit);

            Assert.Equal(Limit, await RecurringIncomeRows.CountOfUserAsync(_app, user.UserId));
            Assert.Equal(Limit, (await RecurringIncomeSteps.ListRowsAsync(user.Client)).Count);
        }

        [Fact]
        public async Task Create_TheTwentyFirstActiveIncome_Answers400LimitReachedAndWritesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            await RecurringIncomeSteps.CreateManyAsync(user.Client, Limit);
            string before = await RecurringIncomeRows.SnapshotAsync(_app);

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Monthly());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_LIMIT_REACHED);
            Assert.Equal(before, await RecurringIncomeRows.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Create_AfterStoppingOne_TakesTheFreedSlotButNotAnotherOne()
        {
            SignedInUser user = await _app.SignUpAsync();
            await RecurringIncomeSteps.CreateManyAsync(user.Client, Limit - 1);
            Guid toStop = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(title: "Old job"));
            await RecurringIncomeRequests.StopAsync(user.Client, toStop);

            HttpResponseMessage takesTheSlot = await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Monthly(title: "New job"));
            HttpResponseMessage oneTooMany = await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Monthly(title: "Extra"));

            Assert.Equal(HttpStatusCode.OK, takesTheSlot.StatusCode);
            await ProblemResponse.AssertAsync(oneTooMany, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_LIMIT_REACHED);
            Assert.Equal(Limit, (await RecurringIncomeSteps.ListRowsAsync(user.Client)).Count);
        }

        [Fact]
        public async Task Create_ResendOfAnAlreadySavedRequestAtTheLimit_Answers200WithTheSavedRow()
        {
            SignedInUser user = await _app.SignUpAsync();
            await RecurringIncomeSteps.CreateManyAsync(user.Client, Limit - 1);
            Guid clientRequestId = Guid.CreateVersion7();
            Guid twentieth = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(title: "Twentieth"), clientRequestId);

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Monthly(title: "Twentieth"), clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(twentieth, (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid());
            Assert.Equal(Limit, await RecurringIncomeRows.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Create_ClientRequestIdOfAnotherUserAtTheLimit_Answers400ClientRequestIdUsedAndNotLimitReached()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            Guid clientRequestId = Guid.CreateVersion7();
            await RecurringIncomeSteps.CreateAsync(owner.Client, RecurringIncomeBody.Monthly(), clientRequestId);
            await RecurringIncomeSteps.CreateManyAsync(other.Client, Limit);

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(other.Client, RecurringIncomeBody.Monthly(), clientRequestId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_CLIENT_REQUEST_ID_USED);
        }

        [Fact]
        public async Task Create_AnotherUserWhileTheFirstIsAtTheLimit_Answers200()
        {
            SignedInUser full = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            await RecurringIncomeSteps.CreateManyAsync(full.Client, Limit);

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(other.Client, RecurringIncomeBody.Monthly());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Edit_AnIncomeAtTheLimit_Answers200()
        {
            SignedInUser user = await _app.SignUpAsync();
            await RecurringIncomeSteps.CreateManyAsync(user.Client, Limit - 1);
            Guid twentieth = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly());

            HttpResponseMessage response = await RecurringIncomeRequests.EditAsync(user.Client, twentieth, RecurringIncomeBody.Monthly(5_000_000));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Restart_AStoppedIncomeWhileTwentyOthersAreActive_Answers400LimitReachedAndStaysStopped()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid stopped = await RecurringIncomeSteps.CreateAndStopAsync(user.Client, RecurringIncomeBody.Monthly(title: "Old job"));
            await RecurringIncomeSteps.CreateManyAsync(user.Client, Limit);

            HttpResponseMessage response = await RecurringIncomeRequests.RestartAsync(user.Client, stopped);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_LIMIT_REACHED);
            Assert.NotNull(await RecurringIncomeRows.ValueAsync(_app, stopped, "stopped_at"));
        }
    }
}
