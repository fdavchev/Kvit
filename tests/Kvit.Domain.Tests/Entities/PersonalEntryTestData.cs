using System.Globalization;
using Kvit.Domain.Entities;
using Kvit.Domain.ExchangeRates;
using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;
using Kvit.Domain.Services.ExchangeRates;
using Kvit.Domain.Tests.Fakes;

namespace Kvit.Domain.Tests.Entities
{
    public static class PersonalEntryTestData
    {
        public const long OneThousandMkd = 100_000;
        public const string Mkd = "MKD";
        public const string Eur = "EUR";
        public const string EntryDate = "2026-10-18";

        public static readonly Guid UserId = CategoryTestData.OwnerId;
        public static readonly Guid OtherUserId = CategoryTestData.OtherOwnerId;
        public static readonly DateTimeOffset Moment = CategoryTestData.Moment;
        public static readonly DateOnly UtcToday = DateOnly.FromDateTime(Moment.UtcDateTime);
        public static readonly ExchangeRateSnapshot Rate = new(new DateOnly(2026, 10, 19), 61.5000m, Moment);
        public static readonly ExchangeRateSnapshot NewerRate = new(new DateOnly(2026, 10, 21), 62.2500m, Moment.AddDays(2));

        public static PersonalEntryInput Input(Guid categoryId, long amountMinor = OneThousandMkd, string currency = Mkd, string entryDate = EntryDate, string? note = null)
        {
            return new PersonalEntryInput(categoryId, amountMinor, currency, entryDate, note);
        }

        public static PersonalEntryFields Fields(Guid categoryId, long amountMinor = OneThousandMkd, string currency = Mkd, string entryDate = EntryDate, string? note = null)
        {
            Result<PersonalEntryFields> fields = PersonalEntryFields.Create(Input(categoryId, amountMinor, currency, entryDate, note), UtcToday);

            return fields.Value;
        }

        public static IncomeInput IncomeInputOf(long amountMinor = OneThousandMkd, string currency = Mkd, string entryDate = EntryDate, string? note = null)
        {
            return new IncomeInput(amountMinor, currency, entryDate, note);
        }

        public static IncomeFields IncomeFieldsOf(long amountMinor = OneThousandMkd, string currency = Mkd, string entryDate = EntryDate, string? note = null)
        {
            Result<IncomeFields> fields = IncomeFields.Create(IncomeInputOf(amountMinor, currency, entryDate, note), UtcToday);

            return fields.Value;
        }

        public static PersonalEntry NewIncome(Guid? userId = null, string currency = Mkd, long amountMinor = OneThousandMkd)
        {
            return PersonalEntry.CreateIncome(userId ?? UserId, IncomeFieldsOf(amountMinor, currency), Rate, Guid.CreateVersion7(), Moment);
        }

        public static PersonalEntry NewSpending(Guid categoryId, Guid? userId = null, string currency = Mkd, long amountMinor = OneThousandMkd)
        {
            return PersonalEntry.CreateSpending(userId ?? UserId, Fields(categoryId, amountMinor, currency), Rate, Guid.CreateVersion7(), Moment);
        }

        public static PersonalEntry OldSpending(Guid categoryId, DateOnly entryDate)
        {
            string entryDateText = entryDate.ToString(ExpenseFields.DateFormat, CultureInfo.InvariantCulture);
            Result<PersonalEntryFields> fields = PersonalEntryFields.CreateForEdit(Input(categoryId, entryDate: entryDateText), UtcToday, entryDate);

            return PersonalEntry.CreateSpending(UserId, fields.Value, Rate, Guid.CreateVersion7(), Moment);
        }

        public static GetCurrentExchangeRate CurrentRateFrom(FakeExchangeRateRepository rates, FakeExchangeRateSource source, TimeProvider timeProvider)
        {
            return new GetCurrentExchangeRate(rates, source, new ExchangeRateRefreshGate(timeProvider), timeProvider, new RecordingLogger<GetCurrentExchangeRate>());
        }

        public static void SeedRate(FakeExchangeRateRepository rates, ExchangeRateSnapshot rate)
        {
            rates.Seed(rate.RateDate, rate.MkdPerEur, rate.FetchedAt);
        }
    }
}
