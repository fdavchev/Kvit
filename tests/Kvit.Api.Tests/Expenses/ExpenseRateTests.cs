using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class ExpenseRateTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly DateOnly FreshRateDate = new(2026, 10, 2);
        private static readonly DateOnly OlderRateDate = new(2026, 10, 1);
        private const decimal FreshRate = 61.5000m;

        [Theory]
        [InlineData("MKD", 100_000)]
        [InlineData("EUR", 1_000)]
        public async Task Add_NewestRateFetchedOneHourAgo_SavesThatRateAndDoesNotAskTheSource(string currency, long amountMinor)
        {
            await _app.PrepareRatesAsync(FreshRateDate, FreshRate, TimeSpan.FromHours(1));
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(amountMinor, currency, group.OwnerRowId, group.OwnerRowId));

            Assert.Empty(_app.RateSource.RequestedDates);
            Assert.Equal("61.5000", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "mkd_per_eur"));
            Assert.Equal("2026-10-02", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "rate_date"));
        }

        [Fact]
        public async Task Add_NewestRateFetchedOneHourAgo_AnswersTheSavedRateAndItsDate()
        {
            await _app.PrepareRatesAsync(FreshRateDate, FreshRate, TimeSpan.FromHours(1));
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, ExpenseInputs.Equal(100_000, "MKD", group.OwnerRowId, group.OwnerRowId));

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(FreshRate, body.GetProperty("mkdPerEur").GetDecimal());
            Assert.Equal("2026-10-02", body.GetProperty("rateDate").GetString());
        }

        [Fact]
        public async Task Add_NewestRateFetchedJustUnderEightHoursAgo_DoesNotAskTheSource()
        {
            await _app.PrepareRatesAsync(FreshRateDate, FreshRate, TimeSpan.FromHours(8) - TimeSpan.FromMinutes(1));
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(100_000, "MKD", group.OwnerRowId, group.OwnerRowId));

            Assert.Empty(_app.RateSource.RequestedDates);
        }

        [Theory]
        [InlineData("MKD", 100_000)]
        [InlineData("EUR", 1_000)]
        public async Task Add_NewestRateFetchedNineHoursAgo_AsksTheSourceOnceForUtcTodayAndSavesItsAnswer(string currency, long amountMinor)
        {
            await _app.PrepareRatesAsync(OlderRateDate, FreshRate, TimeSpan.FromHours(9));
            _app.RateSource.AnswerWith(FreshRateDate, 61.8123m);
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(amountMinor, currency, group.OwnerRowId, group.OwnerRowId));

            Assert.Equal([_app.Clock.UtcToday], _app.RateSource.RequestedDates);
            Assert.Equal("61.8123", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "mkd_per_eur"));
            Assert.Equal("2026-10-02", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "rate_date"));
        }

        [Fact]
        public async Task Add_NewestRateFetchedExactlyEightHoursAgo_AsksTheSource()
        {
            await _app.PrepareRatesAsync(OlderRateDate, FreshRate, TimeSpan.FromHours(8));
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(100_000, "MKD", group.OwnerRowId, group.OwnerRowId));

            Assert.Single(_app.RateSource.RequestedDates);
        }

        [Fact]
        public async Task Add_NewestRateFetchedNineHoursAgo_AnswersTheFetchedRateAndItsDate()
        {
            await _app.PrepareRatesAsync(OlderRateDate, FreshRate, TimeSpan.FromHours(9));
            _app.RateSource.AnswerWith(FreshRateDate, 61.8123m);
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, ExpenseInputs.Equal(100_000, "MKD", group.OwnerRowId, group.OwnerRowId));

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(61.8123m, body.GetProperty("mkdPerEur").GetDecimal());
            Assert.Equal("2026-10-02", body.GetProperty("rateDate").GetString());
        }

        [Fact]
        public async Task Add_NewestRateFetchedNineHoursAgo_KeepsTheFetchedRateInTheRatesTable()
        {
            await _app.PrepareRatesAsync(OlderRateDate, FreshRate, TimeSpan.FromHours(9));
            _app.RateSource.AnswerWith(FreshRateDate, 61.8123m);
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(100_000, "MKD", group.OwnerRowId, group.OwnerRowId));

            List<string?> saved = await _app.QueryAsync("SELECT mkd_per_eur::text FROM exchange_rates WHERE rate_date = '2026-10-02'");
            Assert.Equal("61.8123", Assert.Single(saved));
        }

        [Fact]
        public async Task Add_FetchedRateForTheDateOfTheNewestRow_ReplacesThatRowInsteadOfAddingOne()
        {
            await _app.PrepareRatesAsync(FreshRateDate, FreshRate, TimeSpan.FromHours(9));
            _app.RateSource.AnswerWith(FreshRateDate, 61.8123m);
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(100_000, "MKD", group.OwnerRowId, group.OwnerRowId));

            List<string?> rows = await _app.QueryAsync("SELECT mkd_per_eur::text FROM exchange_rates");
            Assert.Equal("61.8123", Assert.Single(rows));
            Assert.Equal("61.8123", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "mkd_per_eur"));
        }

        [Fact]
        public async Task Add_TwoExpensesAfterARefresh_AskTheSourceOnlyOnce()
        {
            await _app.PrepareRatesAsync(OlderRateDate, FreshRate, TimeSpan.FromHours(9));
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(100_000, "MKD", group.OwnerRowId, group.OwnerRowId);

            await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            Guid second = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            Assert.Single(_app.RateSource.RequestedDates);
            Assert.Equal("61.7000", await ExpenseRows.ExpenseValueAsync(_app, second, "mkd_per_eur"));
        }

        [Fact]
        public async Task Edit_WithTheCurrencyUnchanged_KeepsTheRateSavedWithTheExpense()
        {
            await _app.PrepareRatesAsync(FreshRateDate, FreshRate, TimeSpan.FromHours(1));
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(100_000, "MKD", group.OwnerRowId, group.OwnerRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await _app.PrepareRatesAsync(new DateOnly(2026, 10, 4), 62.2500m, TimeSpan.FromHours(1));

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { AmountMinor = 150_000 });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("61.5000", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "mkd_per_eur"));
            Assert.Equal("2026-10-02", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "rate_date"));
        }

        [Fact]
        public async Task Edit_WithTheCurrencyUnchangedWhileTheNewestRateIsStale_KeepsTheRateSavedWithTheExpense()
        {
            await _app.PrepareRatesAsync(FreshRateDate, FreshRate, TimeSpan.FromHours(1));
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(100_000, "MKD", group.OwnerRowId, group.OwnerRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await _app.PrepareRatesAsync(new DateOnly(2026, 10, 4), 62.2500m, TimeSpan.FromHours(20));

            await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { Title = "Renamed" });

            Assert.Equal("61.5000", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "mkd_per_eur"));
            Assert.Equal("2026-10-02", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "rate_date"));
        }

        [Fact]
        public async Task Edit_WithTheCurrencyChanged_SavesTheCurrentRate()
        {
            await _app.PrepareRatesAsync(FreshRateDate, FreshRate, TimeSpan.FromHours(1));
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(100_000, "MKD", group.OwnerRowId, group.OwnerRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await _app.PrepareRatesAsync(new DateOnly(2026, 10, 4), 62.2500m, TimeSpan.FromHours(1));

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { Currency = "EUR" });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("62.2500", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "mkd_per_eur"));
            Assert.Equal("2026-10-04", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "rate_date"));
        }

        [Fact]
        public async Task Edit_WithTheCurrencyChangedWhileTheNewestRateIsStale_SavesTheFetchedRate()
        {
            await _app.PrepareRatesAsync(FreshRateDate, FreshRate, TimeSpan.FromHours(1));
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(100_000, "MKD", group.OwnerRowId, group.OwnerRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await _app.PrepareRatesAsync(new DateOnly(2026, 10, 3), 62.2500m, TimeSpan.FromHours(20));
            _app.RateSource.AnswerWith(new DateOnly(2026, 10, 4), 62.9000m);

            await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { Currency = "EUR" });

            Assert.Equal("62.9000", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "mkd_per_eur"));
            Assert.Equal("2026-10-04", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "rate_date"));
        }
    }
}
