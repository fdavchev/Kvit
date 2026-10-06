using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class ExpenseRateFailureTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly DateOnly KeptRateDate = new(2026, 10, 1);
        private const decimal KeptRate = 61.5000m;
        private static readonly TimeSpan PastTheRetryWindow = TimeSpan.FromHours(1);

        [Fact]
        public async Task Add_WhenTheSourceFailsWhileTheRateIsStale_SavesTheExpenseWithTheKeptRateAndItsDate()
        {
            ExpenseGroup group = await PrepareFailingSourceAsync();

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, ExpenseInputs.Equal(100_000, "MKD", group.OwnerRowId, group.OwnerRowId));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Guid expenseId = (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid();
            Assert.Equal([_app.Clock.UtcToday], _app.RateSource.RequestedDates);
            Assert.Equal("61.5000", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "mkd_per_eur"));
            Assert.Equal("2026-10-01", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "rate_date"));
        }

        [Fact]
        public async Task Add_WhenTheSourceFailsForAnEurExpense_AlsoSavesTheKeptRate()
        {
            ExpenseGroup group = await PrepareFailingSourceAsync();

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(1_000, "EUR", group.OwnerRowId, group.OwnerRowId));

            Assert.Equal("61.5000", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "mkd_per_eur"));
            Assert.Equal("2026-10-01", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "rate_date"));
        }

        [Fact]
        public async Task Add_WhenTheSourceFailsTheRatesTableKeepsItsOnlyRowUnchanged()
        {
            ExpenseGroup group = await PrepareFailingSourceAsync();

            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(100_000, "MKD", group.OwnerRowId, group.OwnerRowId));

            List<string?> rows = await _app.QueryAsync("SELECT rate_date::text || ' ' || mkd_per_eur::text FROM exchange_rates");
            Assert.Equal("2026-10-01 61.5000", Assert.Single(rows));
        }

        [Fact]
        public async Task Add_TwoExpensesWithinThirtyMinutesOfAFailure_AskTheSourceOnlyOnce()
        {
            ExpenseGroup group = await PrepareFailingSourceAsync();
            ExpenseInput input = ExpenseInputs.Equal(100_000, "MKD", group.OwnerRowId, group.OwnerRowId);

            await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            _app.Clock.Advance(TimeSpan.FromMinutes(29));
            await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            Assert.Single(_app.RateSource.RequestedDates);
        }

        [Fact]
        public async Task Add_AfterMoreThanThirtyMinutesSinceAFailure_AsksTheSourceAgain()
        {
            ExpenseGroup group = await PrepareFailingSourceAsync();
            ExpenseInput input = ExpenseInputs.Equal(100_000, "MKD", group.OwnerRowId, group.OwnerRowId);
            await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            _app.Clock.Advance(TimeSpan.FromMinutes(31));
            await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            Assert.Equal(2, _app.RateSource.RequestedDates.Count);
        }

        [Fact]
        public async Task Add_WhenTheSourceRecoversAfterTheRetryWindow_SavesTheNewRate()
        {
            ExpenseGroup group = await PrepareFailingSourceAsync();
            ExpenseInput input = ExpenseInputs.Equal(100_000, "MKD", group.OwnerRowId, group.OwnerRowId);
            await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            _app.Clock.Advance(TimeSpan.FromMinutes(31));
            _app.RateSource.AnswerWith(new DateOnly(2026, 10, 5), 62.0000m);

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            Assert.Equal("62.0000", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "mkd_per_eur"));
            Assert.Equal("2026-10-05", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "rate_date"));
        }

        private async Task<ExpenseGroup> PrepareFailingSourceAsync()
        {
            _app.Clock.Advance(PastTheRetryWindow);
            await _app.PrepareRatesAsync(KeptRateDate, KeptRate, TimeSpan.FromHours(9));
            _app.RateSource.Fail();

            return await _app.CreateExpenseGroupAsync();
        }
    }
}
