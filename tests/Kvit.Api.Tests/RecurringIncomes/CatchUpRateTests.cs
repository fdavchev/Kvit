using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.RecurringIncomes
{
    public class CatchUpRateTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly StoredRate SeptemberRate = new(new DateOnly(2026, 9, 10), 61.0000m);
        private static readonly StoredRate EarlyOctoberRate = new(new DateOnly(2026, 10, 1), 62.0000m);
        private static readonly StoredRate MidOctoberRate = new(new DateOnly(2026, 10, 10), 63.0000m);

        [Fact]
        public async Task CatchUp_EntryDate_SavesTheNewestStoredRateOnOrBeforeThatDate()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await CatchUpSteps.StoreRatesAsync(_app, TimeSpan.FromHours(1), SeptemberRate, EarlyOctoberRate, MidOctoberRate);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 9, 1));

            await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(["2026-09-15 2026-09-10 61.0000", "2026-10-15 2026-10-10 63.0000"], await CatchUpSteps.RatesSavedAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_EntryDateBetweenTwoStoredRates_TakesTheEarlierOneNotTheNearestOne()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await CatchUpSteps.StoreRatesAsync(_app, TimeSpan.FromHours(1), SeptemberRate, MidOctoberRate);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 9, startsOn: new DateOnly(2026, 10, 1));

            await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(["2026-10-09 2026-09-10 61.0000"], await CatchUpSteps.RatesSavedAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_EntryDateEqualToAStoredRateDate_TakesThatRate()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await CatchUpSteps.StoreRatesAsync(_app, TimeSpan.FromHours(1), SeptemberRate, EarlyOctoberRate, MidOctoberRate);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 10, startsOn: new DateOnly(2026, 10, 1));

            await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(["2026-10-10 2026-10-10 63.0000"], await CatchUpSteps.RatesSavedAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_EntryDatesBeforeEveryStoredRate_TakeTheOldestStoredRate()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await CatchUpSteps.StoreRatesAsync(_app, TimeSpan.FromHours(1), SeptemberRate, EarlyOctoberRate, MidOctoberRate);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 5, startsOn: new DateOnly(2026, 8, 1));

            await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(
                ["2026-08-05 2026-09-10 61.0000", "2026-09-05 2026-09-10 61.0000", "2026-10-05 2026-10-01 62.0000"],
                await CatchUpSteps.RatesSavedAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_SingleStoredRateNewerThanEveryEntry_IsUsedForAllOfThem()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await CatchUpSteps.StoreRatesAsync(_app, TimeSpan.FromHours(1), MidOctoberRate);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 1, startsOn: new DateOnly(2026, 8, 1));

            await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(
                ["2026-08-01 2026-10-10 63.0000", "2026-09-01 2026-10-10 63.0000", "2026-10-01 2026-10-10 63.0000"],
                await CatchUpSteps.RatesSavedAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_IncomeInEuro_SavesTheStoredRateToo()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await CatchUpSteps.StoreRatesAsync(_app, TimeSpan.FromHours(1), SeptemberRate, EarlyOctoberRate);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, currency: "EUR", amountMinor: 20_000, dayOfMonth: 15, startsOn: new DateOnly(2026, 10, 1));

            await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(["2026-10-15 2026-10-01 62.0000"], await CatchUpSteps.RatesSavedAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_NewestStoredRateIsStale_NeverAsksTheRateSourceAndStillCreatesTheEntries()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await CatchUpSteps.StoreRatesAsync(_app, TimeSpan.FromHours(20), SeptemberRate, EarlyOctoberRate);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 9, 1));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(2, created);
            Assert.Empty(_app.RateSource.RequestedDates);
            Assert.Equal(["2026-09-15 2026-09-10 61.0000", "2026-10-15 2026-10-01 62.0000"], await CatchUpSteps.RatesSavedAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_RateSourceIsDown_StillCreatesTheEntriesFromTheStoredRates()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await CatchUpSteps.StoreRatesAsync(_app, TimeSpan.FromHours(20), EarlyOctoberRate);
            _app.RateSource.Fail();
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 10, 1));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(1, created);
            Assert.Empty(_app.RateSource.RequestedDates);
            Assert.Equal(["2026-10-15 2026-10-01 62.0000"], await CatchUpSteps.RatesSavedAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_WithNothingToCreate_NeverAsksTheRateSourceEither()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await CatchUpSteps.StoreRatesAsync(_app, TimeSpan.FromHours(20), EarlyOctoberRate);
            await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 25, startsOn: new DateOnly(2026, 10, 1));

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(0, created);
            Assert.Empty(_app.RateSource.RequestedDates);
        }
    }
}
