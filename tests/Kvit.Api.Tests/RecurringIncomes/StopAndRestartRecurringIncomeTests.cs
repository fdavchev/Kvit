using System.Globalization;
using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.RecurringIncomes
{
    public class StopAndRestartRecurringIncomeTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly TimeSpan FiveDays = TimeSpan.FromDays(5);
        private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

        [Fact]
        public async Task Stop_OwnIncome_Answers204AndStoresTheStopMomentFromTheClock()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly());
            string expectedStoppedAt = _app.Clock.GetUtcNow().UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            HttpResponseMessage response = await RecurringIncomeRequests.StopAsync(user.Client, recurringIncomeId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(expectedStoppedAt, await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "stopped_at AT TIME ZONE 'UTC'"));
        }

        [Fact]
        public async Task Stop_OwnIncome_RemovesItFromTheListAndKeepsTheOtherOnes()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid stopped = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(title: "Old job"));
            Guid kept = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Weekly(title: "Rent"));

            await RecurringIncomeRequests.StopAsync(user.Client, stopped);

            Assert.Equal([kept], await RecurringIncomeSteps.ListedIdsAsync(user.Client));
        }

        [Fact]
        public async Task Stop_OwnIncome_ChangesOnlyTheStopMomentAndLeavesEveryEntryAndWritesNoEvent()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(endsOn: "2027-03-31"));
            await RecurringIncomeRows.InsertEntryAsync(_app, user.UserId, recurringIncomeId, new DateOnly(2026, 10, 15));
            string entriesBefore = await RecurringIncomeRows.EntriesSnapshotAsync(_app);
            int eventsBefore = await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId);

            await RecurringIncomeRequests.StopAsync(user.Client, recurringIncomeId);

            Assert.Equal("4500000", await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "amount_minor"));
            Assert.Equal("Salary", await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "title"));
            Assert.Equal("2026-10-01", await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "starts_on"));
            Assert.Equal(entriesBefore, await RecurringIncomeRows.EntriesSnapshotAsync(_app));
            Assert.Equal(eventsBefore, await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Stop_UnknownIncome_Answers404NotFound()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await RecurringIncomeRequests.StopAsync(user.Client, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.RECURRING_INCOME_NOT_FOUND);
        }

        [Fact]
        public async Task Stop_AnIncomeThatIsAlreadyStopped_Answers404NotFoundAndKeepsTheFirstStopMoment()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAndStopAsync(user.Client, RecurringIncomeBody.Monthly());
            _app.Clock.Advance(TimeSpan.FromHours(1));
            string before = await RecurringIncomeRows.SnapshotAsync(_app);

            HttpResponseMessage response = await RecurringIncomeRequests.StopAsync(user.Client, recurringIncomeId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.RECURRING_INCOME_NOT_FOUND);
            Assert.Equal(before, await RecurringIncomeRows.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Restart_AStoppedIncome_Answers204ClearsTheStopMomentAndListsItAgain()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAndStopAsync(user.Client, RecurringIncomeBody.Monthly());

            HttpResponseMessage response = await RecurringIncomeRequests.RestartAsync(user.Client, recurringIncomeId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "stopped_at"));
            Assert.Equal([recurringIncomeId], await RecurringIncomeSteps.ListedIdsAsync(user.Client));
        }

        [Fact]
        public async Task Restart_AnIncomeThatIsNotStopped_Answers400NotStoppedAndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly());
            string before = await RecurringIncomeRows.SnapshotAsync(_app);

            HttpResponseMessage response = await RecurringIncomeRequests.RestartAsync(user.Client, recurringIncomeId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_NOT_STOPPED);
            Assert.Equal(before, await RecurringIncomeRows.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Restart_UnknownIncome_Answers404NotFound()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await RecurringIncomeRequests.RestartAsync(user.Client, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.RECURRING_INCOME_NOT_FOUND);
        }

        [Fact]
        public async Task Restart_OneSecondBeforeFiveDaysSinceTheStop_Answers204()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAndStopAsync(user.Client, RecurringIncomeBody.Monthly());
            await RecurringIncomeRows.SetStoppedAtAsync(_app, recurringIncomeId, _app.Clock.GetUtcNow() - FiveDays + OneSecond);

            HttpResponseMessage response = await RecurringIncomeRequests.RestartAsync(user.Client, recurringIncomeId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await RecurringIncomeRows.ValueAsync(_app, recurringIncomeId, "stopped_at"));
        }

        [Fact]
        public async Task Restart_ExactlyFiveDaysSinceTheStop_Answers400RestartExpiredAndStaysStopped()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAndStopAsync(user.Client, RecurringIncomeBody.Monthly());
            await RecurringIncomeRows.SetStoppedAtAsync(_app, recurringIncomeId, _app.Clock.GetUtcNow() - FiveDays);
            string before = await RecurringIncomeRows.SnapshotAsync(_app);

            HttpResponseMessage response = await RecurringIncomeRequests.RestartAsync(user.Client, recurringIncomeId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_RESTART_EXPIRED);
            Assert.Equal(before, await RecurringIncomeRows.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Restart_OneSecondAfterFiveDaysSinceTheStop_Answers400RestartExpired()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAndStopAsync(user.Client, RecurringIncomeBody.Monthly());
            await RecurringIncomeRows.SetStoppedAtAsync(_app, recurringIncomeId, _app.Clock.GetUtcNow() - FiveDays - OneSecond);

            HttpResponseMessage response = await RecurringIncomeRequests.RestartAsync(user.Client, recurringIncomeId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_RESTART_EXPIRED);
        }

        [Fact]
        public async Task Restart_ThenStopAgain_Answers204BothTimes()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAndStopAsync(user.Client, RecurringIncomeBody.Monthly());
            await RecurringIncomeRequests.RestartAsync(user.Client, recurringIncomeId);

            HttpResponseMessage response = await RecurringIncomeRequests.StopAsync(user.Client, recurringIncomeId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Empty(await RecurringIncomeSteps.ListedIdsAsync(user.Client));
        }

        [Fact]
        public async Task Restart_AStoppedIncome_WritesNoEventAndNoEntry()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAndStopAsync(user.Client, RecurringIncomeBody.Monthly());
            string entriesBefore = await RecurringIncomeRows.EntriesSnapshotAsync(_app);
            int eventsBefore = await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId);

            await RecurringIncomeRequests.RestartAsync(user.Client, recurringIncomeId);

            Assert.Equal(entriesBefore, await RecurringIncomeRows.EntriesSnapshotAsync(_app));
            Assert.Equal(eventsBefore, await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId));
        }
    }
}
