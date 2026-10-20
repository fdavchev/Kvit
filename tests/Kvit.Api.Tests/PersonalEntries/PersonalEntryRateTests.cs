using System.Net;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.PersonalEntries
{
    public class PersonalEntryRateTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly DateOnly FreshRateDate = new(2026, 10, 2);
        private static readonly DateOnly OlderRateDate = new(2026, 10, 1);
        private const decimal FreshRate = 61.5000m;

        [Theory]
        [InlineData("MKD", 100_000)]
        [InlineData("EUR", 1_000)]
        public async Task Add_NewestRateFetchedOneHourAgo_SavesThatRateAndDoesNotAskTheSource(string currency, long amountMinor)
        {
            SignedInUser user = await _app.SignUpAsync();
            await _app.PrepareRatesAsync(FreshRateDate, FreshRate, TimeSpan.FromHours(1));

            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, amountMinor, currency));

            Assert.Empty(_app.RateSource.RequestedDates);
            Assert.Equal("61.5000", await PersonalEntrySteps.ValueAsync(_app, entryId, "mkd_per_eur"));
            Assert.Equal("2026-10-02", await PersonalEntrySteps.ValueAsync(_app, entryId, "rate_date"));
        }

        [Theory]
        [InlineData("MKD", 100_000)]
        [InlineData("EUR", 1_000)]
        public async Task Add_NewestRateFetchedNineHoursAgo_AsksTheSourceOnceForUtcTodayAndSavesItsAnswer(string currency, long amountMinor)
        {
            SignedInUser user = await _app.SignUpAsync();
            await _app.PrepareRatesAsync(OlderRateDate, FreshRate, TimeSpan.FromHours(9));
            _app.RateSource.AnswerWith(FreshRateDate, 61.8123m);

            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, amountMinor, currency));

            Assert.Equal([_app.Clock.UtcToday], _app.RateSource.RequestedDates);
            Assert.Equal("61.8123", await PersonalEntrySteps.ValueAsync(_app, entryId, "mkd_per_eur"));
            Assert.Equal("2026-10-02", await PersonalEntrySteps.ValueAsync(_app, entryId, "rate_date"));
        }

        [Fact]
        public async Task Edit_WithTheCurrencyUnchanged_KeepsTheRateSavedWithTheEntry()
        {
            SignedInUser user = await _app.SignUpAsync();
            await _app.PrepareRatesAsync(FreshRateDate, FreshRate, TimeSpan.FromHours(1));
            EntryInput input = EntryInput.Of(PersonalEntryRequests.BuiltInFoodId);
            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, input);
            await _app.PrepareRatesAsync(new DateOnly(2026, 10, 4), 62.2500m, TimeSpan.FromHours(1));

            HttpResponseMessage response = await PersonalEntryRequests.EditAsync(user.Client, entryId, input with { AmountMinor = 150_000 });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("61.5000", await PersonalEntrySteps.ValueAsync(_app, entryId, "mkd_per_eur"));
            Assert.Equal("2026-10-02", await PersonalEntrySteps.ValueAsync(_app, entryId, "rate_date"));
        }

        [Fact]
        public async Task Edit_WithTheCurrencyUnchangedWhileTheNewestRateIsStale_DoesNotAskTheSource()
        {
            SignedInUser user = await _app.SignUpAsync();
            await _app.PrepareRatesAsync(FreshRateDate, FreshRate, TimeSpan.FromHours(1));
            EntryInput input = EntryInput.Of(PersonalEntryRequests.BuiltInFoodId);
            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, input);
            await _app.PrepareRatesAsync(new DateOnly(2026, 10, 4), 62.2500m, TimeSpan.FromHours(20));

            await PersonalEntryRequests.EditAsync(user.Client, entryId, input with { Note = "Renamed" });

            Assert.Empty(_app.RateSource.RequestedDates);
            Assert.Equal("61.5000", await PersonalEntrySteps.ValueAsync(_app, entryId, "mkd_per_eur"));
        }

        [Fact]
        public async Task Edit_WithTheCurrencyChanged_SavesTheCurrentRate()
        {
            SignedInUser user = await _app.SignUpAsync();
            await _app.PrepareRatesAsync(FreshRateDate, FreshRate, TimeSpan.FromHours(1));
            EntryInput input = EntryInput.Of(PersonalEntryRequests.BuiltInFoodId);
            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, input);
            await _app.PrepareRatesAsync(new DateOnly(2026, 10, 4), 62.2500m, TimeSpan.FromHours(1));

            HttpResponseMessage response = await PersonalEntryRequests.EditAsync(user.Client, entryId, input with { AmountMinor = 1_000, Currency = "EUR" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("62.2500", await PersonalEntrySteps.ValueAsync(_app, entryId, "mkd_per_eur"));
            Assert.Equal("2026-10-04", await PersonalEntrySteps.ValueAsync(_app, entryId, "rate_date"));
        }

        [Fact]
        public async Task Edit_WithTheCurrencyChangedWhileTheNewestRateIsStale_SavesTheFetchedRate()
        {
            SignedInUser user = await _app.SignUpAsync();
            await _app.PrepareRatesAsync(FreshRateDate, FreshRate, TimeSpan.FromHours(1));
            EntryInput input = EntryInput.Of(PersonalEntryRequests.BuiltInFoodId);
            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, input);
            await _app.PrepareRatesAsync(new DateOnly(2026, 10, 3), 62.2500m, TimeSpan.FromHours(20));
            _app.RateSource.AnswerWith(new DateOnly(2026, 10, 4), 62.9000m);

            await PersonalEntryRequests.EditAsync(user.Client, entryId, input with { AmountMinor = 1_000, Currency = "EUR" });

            Assert.Equal("62.9000", await PersonalEntrySteps.ValueAsync(_app, entryId, "mkd_per_eur"));
            Assert.Equal("2026-10-04", await PersonalEntrySteps.ValueAsync(_app, entryId, "rate_date"));
        }
    }
}
