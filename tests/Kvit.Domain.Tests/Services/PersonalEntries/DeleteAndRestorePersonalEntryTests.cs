using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Services.PersonalEntries;
using Kvit.Domain.Tests.Entities;
using Kvit.Domain.Tests.Fakes;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Services.PersonalEntries
{
    public class DeleteAndRestorePersonalEntryTests
    {
        private static readonly TimeSpan FiveDays = TimeSpan.FromDays(5);
        private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

        private readonly FakePersonalEntryRepository _entries = new();
        private readonly AdjustableTimeProvider _clock = new(PersonalEntryTestData.Moment);
        private readonly DeletePersonalEntry _deletePersonalEntry;
        private readonly RestorePersonalEntry _restorePersonalEntry;
        private readonly PersonalEntry _entry = PersonalEntryTestData.NewSpending(Guid.CreateVersion7());

        public DeleteAndRestorePersonalEntryTests()
        {
            _entries.Seed(_entry);
            _deletePersonalEntry = new DeletePersonalEntry(_entries, _clock);
            _restorePersonalEntry = new RestorePersonalEntry(_entries, _clock);
        }

        [Fact]
        public async Task Delete_OwnEntry_StoresTheDeleteMomentFromTheClock()
        {
            Result result = await DeleteAsync(PersonalEntryTestData.UserId, _entry.Id);

            Assert.True(result.IsSuccess);
            Assert.Equal(PersonalEntryTestData.Moment, _entry.DeletedAt);
        }

        [Fact]
        public async Task Delete_EntryOfAnotherUser_FailsWith404NotFoundAndKeepsIt()
        {
            Result result = await DeleteAsync(PersonalEntryTestData.OtherUserId, _entry.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
            Assert.Null(_entry.DeletedAt);
        }

        [Fact]
        public async Task Delete_AlreadyDeletedEntry_FailsWith404NotFoundAndKeepsTheFirstDeleteMoment()
        {
            await DeleteAsync(PersonalEntryTestData.UserId, _entry.Id);
            _clock.Advance(TimeSpan.FromHours(1));

            Result result = await DeleteAsync(PersonalEntryTestData.UserId, _entry.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
            Assert.Equal(PersonalEntryTestData.Moment, _entry.DeletedAt);
        }

        [Fact]
        public async Task Restore_OwnEntryOneSecondBeforeFiveDays_BringsItBack()
        {
            await DeleteAsync(PersonalEntryTestData.UserId, _entry.Id);
            _clock.Advance(FiveDays - OneSecond);

            Result result = await RestoreAsync(PersonalEntryTestData.UserId, _entry.Id);

            Assert.True(result.IsSuccess);
            Assert.Null(_entry.DeletedAt);
        }

        [Fact]
        public async Task Restore_OwnEntryExactlyFiveDaysAfterTheDelete_FailsWithRestoreExpired()
        {
            await DeleteAsync(PersonalEntryTestData.UserId, _entry.Id);
            _clock.Advance(FiveDays);

            Result result = await RestoreAsync(PersonalEntryTestData.UserId, _entry.Id);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_RESTORE_EXPIRED);
            Assert.NotNull(_entry.DeletedAt);
        }

        [Fact]
        public async Task Restore_EntryThatIsNotDeleted_FailsWithNotDeleted()
        {
            Result result = await RestoreAsync(PersonalEntryTestData.UserId, _entry.Id);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_NOT_DELETED);
        }

        [Fact]
        public async Task Restore_DeletedEntryOfAnotherUser_FailsWith404NotFoundAndKeepsItDeleted()
        {
            await DeleteAsync(PersonalEntryTestData.UserId, _entry.Id);

            Result result = await RestoreAsync(PersonalEntryTestData.OtherUserId, _entry.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
            Assert.NotNull(_entry.DeletedAt);
        }

        [Fact]
        public async Task Restore_NotDeletedEntryOfAnotherUser_FailsWith404NotFoundNotNotDeleted()
        {
            Result result = await RestoreAsync(PersonalEntryTestData.OtherUserId, _entry.Id);

            ResultAssert.Failed(result, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
        }

        private Task<Result> DeleteAsync(Guid userId, Guid entryId)
        {
            return _deletePersonalEntry.Execute(userId, entryId, TestContext.Current.CancellationToken);
        }

        private Task<Result> RestoreAsync(Guid userId, Guid entryId)
        {
            return _restorePersonalEntry.Execute(userId, entryId, TestContext.Current.CancellationToken);
        }
    }
}
