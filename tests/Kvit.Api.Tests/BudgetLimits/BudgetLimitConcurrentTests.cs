using System.Net;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.BudgetLimits
{
    public class BudgetLimitConcurrentTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const int Repetitions = 20;
        private const string Table = "budget_limits";
        private static readonly DateOnly September = new(2026, 9, 1);

        [Fact]
        public async Task SetMonth_TwoRequestsAtTheSameMomentWhileNoRowExists_BothAnswer204AndOneRowStays()
        {
            SignedInUser user = await _app.SignUpAsync();
            await using InsertGate gate = await InsertGate.CloseAsync(_app, Table);

            Task<HttpResponseMessage> first = BudgetLimitRequests.SetMonthAsync(user.Client, 800_000);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = BudgetLimitRequests.SetMonthAsync(user.Client, 900_000);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.NoContent], [.. responses.Select(response => response.StatusCode)]);
            List<string> rows = await BudgetLimitSteps.RowsAsync(_app, user.UserId, null);
            Assert.Contains(Assert.Single(rows), new[] { "2026-10-01:800000", "2026-10-01:900000" });
        }

        [Fact]
        public async Task SetCategory_TwoRequestsAtTheSameMomentWhileNoRowExists_BothAnswer204AndOneRowStays()
        {
            SignedInUser user = await _app.SignUpAsync();
            await using InsertGate gate = await InsertGate.CloseAsync(_app, Table);

            Task<HttpResponseMessage> first = BudgetLimitRequests.SetCategoryAsync(user.Client, BudgetLimitSteps.Food, 800_000);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = BudgetLimitRequests.SetCategoryAsync(user.Client, BudgetLimitSteps.Food, 900_000);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.NoContent], [.. responses.Select(response => response.StatusCode)]);
            List<string> rows = await BudgetLimitSteps.RowsAsync(_app, user.UserId, BudgetLimitSteps.Food);
            Assert.Contains(Assert.Single(rows), new[] { "2026-10-01:800000", "2026-10-01:900000" });
        }

        [Fact]
        public async Task SetMonthAndSetCategory_AtTheSameMoment_BothAnswer204AndEachKeepsItsOwnRow()
        {
            SignedInUser user = await _app.SignUpAsync();
            await using InsertGate gate = await InsertGate.CloseAsync(_app, Table);

            Task<HttpResponseMessage> month = BudgetLimitRequests.SetMonthAsync(user.Client, 800_000);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> category = BudgetLimitRequests.SetCategoryAsync(user.Client, BudgetLimitSteps.Food, 900_000);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(month, category);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.NoContent], [.. responses.Select(response => response.StatusCode)]);
            Assert.Equal(["2026-10-01:800000"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, null));
            Assert.Equal(["2026-10-01:900000"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, BudgetLimitSteps.Food));
        }

        [Fact]
        public async Task Delete_TwoRequestsAtTheSameMomentWhileAnEarlierLimitIsInForce_BothAnswer204AndOneNoLimitRowStays()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitRows.InsertAsync(_app, user.UserId, null, September, 1_000_000);
            await BudgetLimitRows.InsertAsync(_app, user.UserId, BudgetLimitSteps.Food, September, 1_000_000);
            await using InsertGate gate = await InsertGate.CloseAsync(_app, Table);

            Task<HttpResponseMessage> firstMonth = BudgetLimitRequests.DeleteMonthAsync(user.Client);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> secondMonth = BudgetLimitRequests.DeleteMonthAsync(user.Client);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(firstMonth, secondMonth);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.NoContent], [.. responses.Select(response => response.StatusCode)]);
            Assert.Equal(["2026-09-01:1000000", "2026-10-01:none"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, null));
        }

        [Fact]
        public async Task DeleteCategory_TwoRequestsAtTheSameMomentWhileAnEarlierLimitIsInForce_BothAnswer204AndOneNoLimitRowStays()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitRows.InsertAsync(_app, user.UserId, BudgetLimitSteps.Food, September, 1_000_000);
            await using InsertGate gate = await InsertGate.CloseAsync(_app, Table);

            Task<HttpResponseMessage> first = BudgetLimitRequests.DeleteCategoryAsync(user.Client, BudgetLimitSteps.Food);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = BudgetLimitRequests.DeleteCategoryAsync(user.Client, BudgetLimitSteps.Food);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.NoContent], [.. responses.Select(response => response.StatusCode)]);
            Assert.Equal(["2026-09-01:1000000", "2026-10-01:none"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, BudgetLimitSteps.Food));
        }

        [Fact]
        public async Task SetMonthAndDeleteMonth_AtTheSameMomentWhileAnEarlierLimitIsInForce_BothAnswer204AndOneRowForThisMonthStays()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitRows.InsertAsync(_app, user.UserId, null, September, 1_000_000);
            await using InsertGate gate = await InsertGate.CloseAsync(_app, Table);

            Task<HttpResponseMessage> set = BudgetLimitRequests.SetMonthAsync(user.Client, 800_000);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> delete = BudgetLimitRequests.DeleteMonthAsync(user.Client);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(set, delete);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.NoContent], [.. responses.Select(response => response.StatusCode)]);
            List<string> rows = await BudgetLimitSteps.RowsAsync(_app, user.UserId, null);
            Assert.Equal(2, rows.Count);
            Assert.Equal("2026-09-01:1000000", rows[0]);
            Assert.Contains(rows[1], new[] { "2026-10-01:800000", "2026-10-01:none" });
        }

        [Fact]
        public async Task Delete_ManyRequestsAtTheSameMomentWhenTheCurrentMonthHasARowAndNothingEarlier_AllAnswer204AndNoRowStays()
        {
            SignedInUser user = await _app.SignUpAsync();

            for (int repetition = 0; repetition < Repetitions; repetition++)
            {
                await BudgetLimitSteps.SetMonthAsync(user, 800_000);

                HttpResponseMessage[] responses = await Task.WhenAll(
                    BudgetLimitRequests.DeleteMonthAsync(user.Client),
                    BudgetLimitRequests.DeleteMonthAsync(user.Client),
                    BudgetLimitRequests.DeleteMonthAsync(user.Client));

                Assert.All(responses, response => Assert.Equal(HttpStatusCode.NoContent, response.StatusCode));
                Assert.Empty(await BudgetLimitSteps.RowsAsync(_app, user.UserId, null));
            }
        }

        [Fact]
        public async Task SetAndDelete_RepeatedAtTheSameMoment_NeverAnswerAnErrorAndLeaveAtMostOneRowPerMonth()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitRows.InsertAsync(_app, user.UserId, null, September, 1_000_000);

            for (int repetition = 0; repetition < Repetitions; repetition++)
            {
                HttpResponseMessage[] responses = await Task.WhenAll(
                    BudgetLimitRequests.SetMonthAsync(user.Client, 800_000),
                    BudgetLimitRequests.DeleteMonthAsync(user.Client),
                    BudgetLimitRequests.SetMonthAsync(user.Client, 900_000),
                    BudgetLimitRequests.DeleteMonthAsync(user.Client));

                Assert.All(responses, response => Assert.Equal(HttpStatusCode.NoContent, response.StatusCode));
                List<string> rows = await BudgetLimitSteps.RowsAsync(_app, user.UserId, null);
                Assert.Equal(2, rows.Count);
                Assert.Equal("2026-09-01:1000000", rows[0]);
                await _app.ExecuteAsync(
                    "DELETE FROM budget_limits WHERE user_id = @user_id AND effective_from = DATE '2026-10-01'",
                    new NpgsqlParameter("user_id", user.UserId));
            }
        }
    }
}
