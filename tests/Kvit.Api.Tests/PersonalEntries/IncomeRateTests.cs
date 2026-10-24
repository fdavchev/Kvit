using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.PersonalEntries
{
    public class IncomeRateTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly DateOnly FreshRateDate = new(2026, 10, 2);
        private static readonly DateOnly OlderRateDate = new(2026, 10, 1);
        private const decimal FreshRate = 61.5000m;

        [Theory]
        [InlineData("MKD", 4_500_000)]
        [InlineData("EUR", 100_000)]
        public async Task Add_IncomeWithTheNewestRateFetchedOneHourAgo_SavesThatRateAndDoesNotAskTheSource(string currency, long amountMinor)
        {
            SignedInUser user = await _app.SignUpAsync();
            await _app.PrepareRatesAsync(FreshRateDate, FreshRate, TimeSpan.FromHours(1));

            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(amountMinor, currency));

            Assert.Empty(_app.RateSource.RequestedDates);
            Assert.Equal("61.5000", await PersonalEntrySteps.ValueAsync(_app, entryId, "mkd_per_eur"));
            Assert.Equal("2026-10-02", await PersonalEntrySteps.ValueAsync(_app, entryId, "rate_date"));
        }

        [Theory]
        [InlineData("MKD", 4_500_000)]
        [InlineData("EUR", 100_000)]
        public async Task Add_IncomeWithTheNewestRateFetchedNineHoursAgo_AsksTheSourceOnceForUtcTodayAndSavesItsAnswer(string currency, long amountMinor)
        {
            SignedInUser user = await _app.SignUpAsync();
            await _app.PrepareRatesAsync(OlderRateDate, FreshRate, TimeSpan.FromHours(9));
            _app.RateSource.AnswerWith(FreshRateDate, 61.8123m);

            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(amountMinor, currency));

            Assert.Equal([_app.Clock.UtcToday], _app.RateSource.RequestedDates);
            Assert.Equal("61.8123", await PersonalEntrySteps.ValueAsync(_app, entryId, "mkd_per_eur"));
            Assert.Equal("2026-10-02", await PersonalEntrySteps.ValueAsync(_app, entryId, "rate_date"));
        }

        [Fact]
        public async Task Add_IncomeWithAnInvalidAmountWhileTheRateIsStaleAndTheSourceDown_Answers400WithTheFieldCodeAndDoesNotAskTheSource()
        {
            SignedInUser user = await _app.SignUpAsync();
            await _app.PrepareRatesAsync(FreshRateDate, FreshRate, TimeSpan.FromHours(20));
            _app.RateSource.Fail();

            HttpResponseMessage response = await PersonalEntryRequests.AddIncomeAsync(user.Client, IncomeEntryInput.Of(0));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_AMOUNT_NOT_POSITIVE);
            Assert.Empty(_app.RateSource.RequestedDates);
        }

        [Fact]
        public async Task Add_IncomeWithACategoryIdWhileTheRateIsStaleAndTheSourceDown_Answers400IncomeHasCategoryAndDoesNotAskTheSource()
        {
            SignedInUser user = await _app.SignUpAsync();
            await _app.PrepareRatesAsync(FreshRateDate, FreshRate, TimeSpan.FromHours(20));
            _app.RateSource.Fail();

            HttpResponseMessage response = await PersonalEntryRequests.AddIncomeWithCategoryAsync(user.Client, IncomeEntryInput.Of(), PersonalEntryRequests.BuiltInFoodId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, PersonalEntryRequests.IncomeHasCategoryCode);
            Assert.Empty(_app.RateSource.RequestedDates);
        }

        [Fact]
        public async Task Edit_IncomeWithTheCurrencyUnchanged_KeepsTheRateSavedWithTheEntry()
        {
            SignedInUser user = await _app.SignUpAsync();
            await _app.PrepareRatesAsync(FreshRateDate, FreshRate, TimeSpan.FromHours(1));
            IncomeEntryInput input = IncomeEntryInput.Of();
            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, input);
            await _app.PrepareRatesAsync(new DateOnly(2026, 10, 4), 62.2500m, TimeSpan.FromHours(1));

            HttpResponseMessage response = await PersonalEntryRequests.EditIncomeAsync(user.Client, entryId, input with { AmountMinor = 5_000_000 });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("61.5000", await PersonalEntrySteps.ValueAsync(_app, entryId, "mkd_per_eur"));
            Assert.Equal("2026-10-02", await PersonalEntrySteps.ValueAsync(_app, entryId, "rate_date"));
        }

        [Fact]
        public async Task Edit_IncomeWithTheCurrencyUnchangedWhileTheNewestRateIsStale_DoesNotAskTheSource()
        {
            SignedInUser user = await _app.SignUpAsync();
            await _app.PrepareRatesAsync(FreshRateDate, FreshRate, TimeSpan.FromHours(1));
            IncomeEntryInput input = IncomeEntryInput.Of();
            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, input);
            await _app.PrepareRatesAsync(new DateOnly(2026, 10, 4), 62.2500m, TimeSpan.FromHours(20));

            await PersonalEntryRequests.EditIncomeAsync(user.Client, entryId, input with { Note = "Renamed" });

            Assert.Empty(_app.RateSource.RequestedDates);
            Assert.Equal("61.5000", await PersonalEntrySteps.ValueAsync(_app, entryId, "mkd_per_eur"));
        }

        [Fact]
        public async Task Edit_IncomeWithTheCurrencyChanged_SavesTheCurrentRate()
        {
            SignedInUser user = await _app.SignUpAsync();
            await _app.PrepareRatesAsync(FreshRateDate, FreshRate, TimeSpan.FromHours(1));
            IncomeEntryInput input = IncomeEntryInput.Of();
            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, input);
            await _app.PrepareRatesAsync(new DateOnly(2026, 10, 4), 62.2500m, TimeSpan.FromHours(1));

            HttpResponseMessage response = await PersonalEntryRequests.EditIncomeAsync(user.Client, entryId, input with { AmountMinor = 100_000, Currency = "EUR" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("62.2500", await PersonalEntrySteps.ValueAsync(_app, entryId, "mkd_per_eur"));
            Assert.Equal("2026-10-04", await PersonalEntrySteps.ValueAsync(_app, entryId, "rate_date"));
        }

        [Fact]
        public async Task Edit_IncomeWithTheCurrencyChangedWhileTheNewestRateIsStale_SavesTheFetchedRate()
        {
            SignedInUser user = await _app.SignUpAsync();
            await _app.PrepareRatesAsync(FreshRateDate, FreshRate, TimeSpan.FromHours(1));
            IncomeEntryInput input = IncomeEntryInput.Of();
            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, input);
            await _app.PrepareRatesAsync(new DateOnly(2026, 10, 3), 62.2500m, TimeSpan.FromHours(20));
            _app.RateSource.AnswerWith(new DateOnly(2026, 10, 4), 62.9000m);

            await PersonalEntryRequests.EditIncomeAsync(user.Client, entryId, input with { AmountMinor = 100_000, Currency = "EUR" });

            Assert.Equal("62.9000", await PersonalEntrySteps.ValueAsync(_app, entryId, "mkd_per_eur"));
            Assert.Equal("2026-10-04", await PersonalEntrySteps.ValueAsync(_app, entryId, "rate_date"));
        }
    }
}
