using System.Globalization;
using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.PersonalEntries
{
    public class DeleteAndRestorePersonalEntryTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly TimeSpan FiveDays = TimeSpan.FromDays(5);
        private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

        [Fact]
        public async Task Delete_OwnEntry_Answers204AndStoresTheDeleteMomentFromTheClock()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));
            string expectedDeletedAt = _app.Clock.GetUtcNow().UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            HttpResponseMessage response = await PersonalEntryRequests.DeleteAsync(user.Client, entryId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Equal(expectedDeletedAt, await PersonalEntrySteps.ValueAsync(_app, entryId, "deleted_at AT TIME ZONE 'UTC'"));
            Assert.Equal(1, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Delete_OwnEntry_WritesNoEvent()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));
            int before = await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId);

            await PersonalEntryRequests.DeleteAsync(user.Client, entryId);
            await PersonalEntryRequests.RestoreAsync(user.Client, entryId);

            Assert.Equal(before, await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Delete_AlreadyDeletedEntry_Answers404NotFoundLikeExpenses()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddAndDeleteAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.DeleteAsync(user.Client, entryId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Delete_UnknownEntry_Answers404NotFound()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.DeleteAsync(user.Client, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
        }

        [Fact]
        public async Task Restore_DeletedOwnEntry_Answers204AndBringsItBackInTheMonth()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddAndDeleteAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));

            HttpResponseMessage response = await PersonalEntryRequests.RestoreAsync(user.Client, entryId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await PersonalEntrySteps.ValueAsync(_app, entryId, "deleted_at"));
            Assert.Equal([entryId], await PersonalEntrySteps.EntryIdsOfMonthAsync(user.Client));
        }

        [Fact]
        public async Task Restore_EntryThatIsNotDeleted_Answers400NotDeletedAndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.RestoreAsync(user.Client, entryId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_NOT_DELETED);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Restore_UnknownEntry_Answers404NotFound()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.RestoreAsync(user.Client, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
        }

        [Fact]
        public async Task Restore_OneSecondBeforeFiveDaysSinceTheDelete_Answers204()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddAndDeleteAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));
            await PersonalEntrySteps.SetDeletedAtAsync(_app, entryId, _app.Clock.GetUtcNow() - FiveDays + OneSecond);

            HttpResponseMessage response = await PersonalEntryRequests.RestoreAsync(user.Client, entryId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await PersonalEntrySteps.ValueAsync(_app, entryId, "deleted_at"));
        }

        [Fact]
        public async Task Restore_ExactlyFiveDaysSinceTheDelete_Answers400RestoreExpiredAndKeepsItDeleted()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddAndDeleteAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));
            await PersonalEntrySteps.SetDeletedAtAsync(_app, entryId, _app.Clock.GetUtcNow() - FiveDays);
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.RestoreAsync(user.Client, entryId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_RESTORE_EXPIRED);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Restore_OneSecondAfterFiveDaysSinceTheDelete_Answers400RestoreExpired()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid entryId = await PersonalEntrySteps.AddAndDeleteAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));
            await PersonalEntrySteps.SetDeletedAtAsync(_app, entryId, _app.Clock.GetUtcNow() - FiveDays - OneSecond);

            HttpResponseMessage response = await PersonalEntryRequests.RestoreAsync(user.Client, entryId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_RESTORE_EXPIRED);
        }
    }
}
