using System.Globalization;
using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.PersonalEntries
{
    public class DeleteAndRestoreIncomeTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly TimeSpan FiveDays = TimeSpan.FromDays(5);
        private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

        [Fact]
        public async Task Delete_OwnIncome_Answers204AndStoresTheDeleteMomentFromTheClock()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of());
            string expectedDeletedAt = _app.Clock.GetUtcNow().UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            HttpResponseMessage response = await PersonalEntryRequests.DeleteAsync(user.Client, entryId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(expectedDeletedAt, await PersonalEntrySteps.ValueAsync(_app, entryId, "deleted_at AT TIME ZONE 'UTC'"));
            Assert.Equal(1, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Delete_OwnIncome_TakesItOutOfTheMonthListAndTheIncomeTotals()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid kept = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(1_000_000));
            Guid deleted = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(3_000_000));

            await PersonalEntryRequests.DeleteAsync(user.Client, deleted);

            JsonElement body = await PersonalEntrySteps.MonthAsync(user.Client);
            Assert.Equal([kept], await PersonalEntrySteps.EntryIdsOfMonthAsync(user.Client));
            JsonElement mkd = Assert.Single(body.GetProperty("incomeTotals").EnumerateArray());
            Assert.Equal(1_000_000, mkd.GetProperty(PersonalEntryRequests.IncomeTotalAmountProperty).GetInt64());
            Assert.Equal(1, mkd.GetProperty("count").GetInt32());
        }

        [Fact]
        public async Task Delete_OwnIncome_WritesNoEvent()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of());
            int before = await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId);

            await PersonalEntryRequests.DeleteAsync(user.Client, entryId);
            await PersonalEntryRequests.RestoreAsync(user.Client, entryId);

            Assert.Equal(before, await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Delete_AlreadyDeletedIncome_Answers404NotFound()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddIncomeAndDeleteAsync(user.Client, IncomeEntryInput.Of());
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.DeleteAsync(user.Client, entryId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Restore_DeletedOwnIncome_Answers204AndBringsItBackInTheMonthAndTheIncomeTotals()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddIncomeAndDeleteAsync(user.Client, IncomeEntryInput.Of(4_500_000));

            HttpResponseMessage response = await PersonalEntryRequests.RestoreAsync(user.Client, entryId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await PersonalEntrySteps.ValueAsync(_app, entryId, "deleted_at"));
            Assert.Equal([entryId], await PersonalEntrySteps.EntryIdsOfMonthAsync(user.Client));
            JsonElement mkd = Assert.Single((await PersonalEntrySteps.MonthAsync(user.Client)).GetProperty("incomeTotals").EnumerateArray());
            Assert.Equal(4_500_000, mkd.GetProperty(PersonalEntryRequests.IncomeTotalAmountProperty).GetInt64());
        }

        [Fact]
        public async Task Restore_IncomeThatIsNotDeleted_Answers400NotDeletedAndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of());
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.RestoreAsync(user.Client, entryId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_NOT_DELETED);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Restore_IncomeOneSecondBeforeFiveDaysSinceTheDelete_Answers204()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddIncomeAndDeleteAsync(user.Client, IncomeEntryInput.Of());
            await PersonalEntrySteps.SetDeletedAtAsync(_app, entryId, _app.Clock.GetUtcNow() - FiveDays + OneSecond);

            HttpResponseMessage response = await PersonalEntryRequests.RestoreAsync(user.Client, entryId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await PersonalEntrySteps.ValueAsync(_app, entryId, "deleted_at"));
        }

        [Fact]
        public async Task Restore_IncomeExactlyFiveDaysSinceTheDelete_Answers400RestoreExpiredAndKeepsItDeleted()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddIncomeAndDeleteAsync(user.Client, IncomeEntryInput.Of());
            await PersonalEntrySteps.SetDeletedAtAsync(_app, entryId, _app.Clock.GetUtcNow() - FiveDays);
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.RestoreAsync(user.Client, entryId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_RESTORE_EXPIRED);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Restore_IncomeOneSecondAfterFiveDaysSinceTheDelete_Answers400RestoreExpired()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddIncomeAndDeleteAsync(user.Client, IncomeEntryInput.Of());
            await PersonalEntrySteps.SetDeletedAtAsync(_app, entryId, _app.Clock.GetUtcNow() - FiveDays - OneSecond);

            HttpResponseMessage response = await PersonalEntryRequests.RestoreAsync(user.Client, entryId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_RESTORE_EXPIRED);
        }
    }
}
