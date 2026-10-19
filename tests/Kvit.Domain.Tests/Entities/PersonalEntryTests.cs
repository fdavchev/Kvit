using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class PersonalEntryTests
    {
        private static readonly Guid CategoryId = Guid.CreateVersion7();
        private static readonly TimeSpan FiveDays = TimeSpan.FromDays(5);
        private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

        [Fact]
        public void CreateSpending_ValidFields_ReturnsASpendingEntryOfTheUserWithTheGivenValues()
        {
            Guid clientRequestId = Guid.CreateVersion7();

            PersonalEntry entry = PersonalEntry.CreateSpending(
                PersonalEntryTestData.UserId,
                PersonalEntryTestData.Fields(CategoryId, 1_550, "EUR", "2026-10-18", "Lunch"),
                PersonalEntryTestData.Rate,
                clientRequestId,
                PersonalEntryTestData.Moment);

            Assert.NotEqual(Guid.Empty, entry.Id);
            Assert.Equal(PersonalEntryTestData.UserId, entry.UserId);
            Assert.Equal(PersonalEntryKind.Spending, entry.Kind);
            Assert.Equal(CategoryId, entry.CategoryId);
            Assert.Equal(1_550, entry.AmountMinor);
            Assert.Equal(Currency.EUR, entry.Currency);
            Assert.Equal(new DateOnly(2026, 10, 18), entry.EntryDate);
            Assert.Equal("Lunch", entry.Note);
            Assert.Equal(clientRequestId, entry.ClientRequestId);
            Assert.Equal(PersonalEntryTestData.Moment, entry.CreatedAt);
            Assert.Null(entry.UpdatedAt);
            Assert.Null(entry.DeletedAt);
        }

        [Fact]
        public void CreateSpending_TheRate_IsSavedWithTheEntry()
        {
            PersonalEntry entry = PersonalEntryTestData.NewSpending(CategoryId);

            Assert.Equal(PersonalEntryTestData.Rate.MkdPerEur, entry.MkdPerEur);
            Assert.Equal(PersonalEntryTestData.Rate.RateDate, entry.RateDate);
        }

        [Fact]
        public void CreateSpending_TwoEntries_GetDifferentIds()
        {
            PersonalEntry first = PersonalEntryTestData.NewSpending(CategoryId);
            PersonalEntry second = PersonalEntryTestData.NewSpending(CategoryId);

            Assert.NotEqual(first.Id, second.Id);
        }

        [Fact]
        public void Update_NewFields_ReplacesTheFieldsAndStoresTheUpdateMomentButKeepsTheRate()
        {
            PersonalEntry entry = PersonalEntryTestData.NewSpending(CategoryId);
            Guid otherCategoryId = Guid.CreateVersion7();
            DateTimeOffset later = PersonalEntryTestData.Moment.AddHours(1);

            entry.Update(PersonalEntryTestData.Fields(otherCategoryId, 2_000, "EUR", "2026-10-17", "Taxi"), later);

            Assert.Equal(otherCategoryId, entry.CategoryId);
            Assert.Equal(2_000, entry.AmountMinor);
            Assert.Equal(Currency.EUR, entry.Currency);
            Assert.Equal(new DateOnly(2026, 10, 17), entry.EntryDate);
            Assert.Equal("Taxi", entry.Note);
            Assert.Equal(later, entry.UpdatedAt);
            Assert.Equal(PersonalEntryTestData.Rate.MkdPerEur, entry.MkdPerEur);
            Assert.Equal(PersonalEntryTestData.Rate.RateDate, entry.RateDate);
        }

        [Fact]
        public void SaveRate_ANewerRate_ReplacesTheSavedRateAndItsDate()
        {
            PersonalEntry entry = PersonalEntryTestData.NewSpending(CategoryId);

            entry.SaveRate(PersonalEntryTestData.NewerRate);

            Assert.Equal(PersonalEntryTestData.NewerRate.MkdPerEur, entry.MkdPerEur);
            Assert.Equal(PersonalEntryTestData.NewerRate.RateDate, entry.RateDate);
        }

        [Fact]
        public void Delete_AnEntry_StoresTheDeleteMoment()
        {
            PersonalEntry entry = PersonalEntryTestData.NewSpending(CategoryId);

            entry.Delete(PersonalEntryTestData.Moment.AddHours(2));

            Assert.Equal(PersonalEntryTestData.Moment.AddHours(2), entry.DeletedAt);
        }

        [Fact]
        public void Restore_NotDeleted_FailsWithNotDeleted()
        {
            PersonalEntry entry = PersonalEntryTestData.NewSpending(CategoryId);

            Result result = entry.Restore(PersonalEntryTestData.Moment);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_NOT_DELETED);
        }

        [Fact]
        public void Restore_OneSecondBeforeFiveDaysSinceTheDelete_ClearsTheDeleteMoment()
        {
            PersonalEntry entry = PersonalEntryTestData.NewSpending(CategoryId);
            entry.Delete(PersonalEntryTestData.Moment);

            Result result = entry.Restore(PersonalEntryTestData.Moment + FiveDays - OneSecond);

            Assert.True(result.IsSuccess);
            Assert.Null(entry.DeletedAt);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void Restore_FiveDaysOrMoreSinceTheDelete_FailsWithRestoreExpiredAndStaysDeleted(int extraSeconds)
        {
            PersonalEntry entry = PersonalEntryTestData.NewSpending(CategoryId);
            entry.Delete(PersonalEntryTestData.Moment);

            Result result = entry.Restore(PersonalEntryTestData.Moment + FiveDays + TimeSpan.FromSeconds(extraSeconds));

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_RESTORE_EXPIRED);
            Assert.Equal(PersonalEntryTestData.Moment, entry.DeletedAt);
        }

        [Fact]
        public void RestorableUntil_IsTheSameWindowAsForExpenses()
        {
            Assert.Equal(Expense.RestorableUntil(PersonalEntryTestData.Moment), PersonalEntry.RestorableUntil(PersonalEntryTestData.Moment));
        }
    }
}
