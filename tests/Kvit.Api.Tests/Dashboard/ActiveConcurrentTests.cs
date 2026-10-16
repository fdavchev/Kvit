using System.Globalization;
using System.Net;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Dashboard
{
    public class ActiveConcurrentTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const int Days = 10;
        private const int CallsAtTheSameMoment = 4;

        [Fact]
        public async Task Active_TwoCallsAtTheSameMoment_BothAnswer204AndExactlyOneRowIsWritten()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage[] responses = await Task.WhenAll(
                DashboardRequests.ActiveAsync(user.Client),
                DashboardRequests.ActiveAsync(user.Client));

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.NoContent], [.. responses.Select(response => response.StatusCode)]);
            Assert.Equal(1, await ActiveRows.CountOfAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Active_TwoCallsBothHeldAtTheInsert_BothAnswer204AndTheSecondInsertHitsTheUniqueIndex()
        {
            SignedInUser user = await _app.SignUpAsync();
            await using InsertGate gate = await InsertGate.CloseAsync(_app, "usage_events", "NEW.type = 'Active'");

            Task<HttpResponseMessage> first = DashboardRequests.ActiveAsync(user.Client);
            Task<HttpResponseMessage> second = DashboardRequests.ActiveAsync(user.Client);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.NoContent], [.. responses.Select(response => response.StatusCode)]);
            Assert.Equal([_app.Clock.UtcToday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)], await ActiveRows.DaysOfAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Active_SeveralCallsAtTheSameMomentOnTenDays_WriteExactlyOneRowPerDay()
        {
            SignedInUser user = await _app.SignUpAsync();

            for (int day = 1; day <= Days; day++)
            {
                _app.Clock.Advance(TimeSpan.FromDays(1));

                HttpResponseMessage[] responses = await Task.WhenAll(
                    Enumerable.Range(0, CallsAtTheSameMoment).Select(_ => DashboardRequests.ActiveAsync(user.Client)));

                Assert.All(responses, response => Assert.Equal(HttpStatusCode.NoContent, response.StatusCode));
                Assert.Equal(day, await ActiveRows.CountOfAsync(_app, user.UserId));
            }

            Assert.Equal(Days, (await ActiveRows.DaysOfAsync(_app, user.UserId)).Distinct().Count());
        }
    }
}
