using System.Net;
using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Kvit.Domain.Tests.Results;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public class PersonalEntryIncomeTests
    {
        private static readonly TimeSpan FiveDays = TimeSpan.FromDays(5);
        private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

        [Fact]
        public void CreateIncome_ValidFields_ReturnsAnIncomeEntryOfTheUserWithNoCategory()
        {
            Guid clientRequestId = Guid.CreateVersion7();

            PersonalEntry entry = PersonalEntry.CreateIncome(
                PersonalEntryTestData.UserId,
                PersonalEntryTestData.IncomeFieldsOf(4_500_000, "MKD", "2026-10-18", "Salary"),
                PersonalEntryTestData.Rate,
                clientRequestId,
                PersonalEntryTestData.Moment);

            Assert.NotEqual(Guid.Empty, entry.Id);
            Assert.Equal(PersonalEntryTestData.UserId, entry.UserId);
            Assert.Equal(PersonalEntryKind.Income, entry.Kind);
            Assert.Null(entry.CategoryId);
            Assert.Equal(4_500_000, entry.AmountMinor);
            Assert.Equal(Currency.MKD, entry.Currency);
            Assert.Equal(new DateOnly(2026, 10, 18), entry.EntryDate);
            Assert.Equal("Salary", entry.Note);
            Assert.Equal(clientRequestId, entry.ClientRequestId);
            Assert.Equal(PersonalEntryTestData.Moment, entry.CreatedAt);
            Assert.Null(entry.UpdatedAt);
            Assert.Null(entry.DeletedAt);
        }

        [Fact]
        public void CreateIncome_InEuro_KeepsTheCurrencyAndTheCents()
        {
            PersonalEntry entry = PersonalEntryTestData.NewIncome(currency: "EUR", amountMinor: 123_456);

            Assert.Equal(Currency.EUR, entry.Currency);
            Assert.Equal(123_456, entry.AmountMinor);
        }

        [Fact]
        public void CreateIncome_TheRate_IsSavedWithTheEntry()
        {
            PersonalEntry entry = PersonalEntryTestData.NewIncome();

            Assert.Equal(PersonalEntryTestData.Rate.MkdPerEur, entry.MkdPerEur);
            Assert.Equal(PersonalEntryTestData.Rate.RateDate, entry.RateDate);
        }

        [Fact]
        public void CreateIncome_TwoEntries_GetDifferentIds()
        {
            PersonalEntry first = PersonalEntryTestData.NewIncome();
            PersonalEntry second = PersonalEntryTestData.NewIncome();

            Assert.NotEqual(first.Id, second.Id);
        }

        [Fact]
        public void Update_NewIncomeFields_ReplacesTheFieldsAndStoresTheUpdateMomentButKeepsTheKindTheNoCategoryAndTheRate()
        {
            PersonalEntry entry = PersonalEntryTestData.NewIncome();
            DateTimeOffset later = PersonalEntryTestData.Moment.AddHours(1);

            entry.Update(PersonalEntryTestData.IncomeFieldsOf(2_000, "EUR", "2026-10-17", "Bonus"), later);

            Assert.Equal(PersonalEntryKind.Income, entry.Kind);
            Assert.Null(entry.CategoryId);
            Assert.Equal(2_000, entry.AmountMinor);
            Assert.Equal(Currency.EUR, entry.Currency);
            Assert.Equal(new DateOnly(2026, 10, 17), entry.EntryDate);
            Assert.Equal("Bonus", entry.Note);
            Assert.Equal(later, entry.UpdatedAt);
            Assert.Equal(PersonalEntryTestData.Rate.MkdPerEur, entry.MkdPerEur);
            Assert.Equal(PersonalEntryTestData.Rate.RateDate, entry.RateDate);
        }

        [Fact]
        public void SaveRate_OnAnIncomeEntry_ReplacesTheSavedRateAndItsDate()
        {
            PersonalEntry entry = PersonalEntryTestData.NewIncome();

            entry.SaveRate(PersonalEntryTestData.NewerRate);

            Assert.Equal(PersonalEntryTestData.NewerRate.MkdPerEur, entry.MkdPerEur);
            Assert.Equal(PersonalEntryTestData.NewerRate.RateDate, entry.RateDate);
        }

        [Fact]
        public void Delete_AnIncomeEntry_StoresTheDeleteMoment()
        {
            PersonalEntry entry = PersonalEntryTestData.NewIncome();

            entry.Delete(PersonalEntryTestData.Moment.AddHours(2));

            Assert.Equal(PersonalEntryTestData.Moment.AddHours(2), entry.DeletedAt);
        }

        [Fact]
        public void Restore_AnIncomeEntryOneSecondBeforeFiveDaysSinceTheDelete_ClearsTheDeleteMoment()
        {
            PersonalEntry entry = PersonalEntryTestData.NewIncome();
            entry.Delete(PersonalEntryTestData.Moment);

            Result result = entry.Restore(PersonalEntryTestData.Moment + FiveDays - OneSecond);

            Assert.True(result.IsSuccess);
            Assert.Null(entry.DeletedAt);
        }

        [Fact]
        public void Restore_AnIncomeEntryExactlyFiveDaysSinceTheDelete_FailsWithRestoreExpiredAndStaysDeleted()
        {
            PersonalEntry entry = PersonalEntryTestData.NewIncome();
            entry.Delete(PersonalEntryTestData.Moment);

            Result result = entry.Restore(PersonalEntryTestData.Moment + FiveDays);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_RESTORE_EXPIRED);
            Assert.Equal(PersonalEntryTestData.Moment, entry.DeletedAt);
        }

        [Fact]
        public void Restore_AnIncomeEntryThatIsNotDeleted_FailsWithNotDeleted()
        {
            PersonalEntry entry = PersonalEntryTestData.NewIncome();

            Result result = entry.Restore(PersonalEntryTestData.Moment);

            ResultAssert.Failed(result, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_NOT_DELETED);
        }
    }
}
