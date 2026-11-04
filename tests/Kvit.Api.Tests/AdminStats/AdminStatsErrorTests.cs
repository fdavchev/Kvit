using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Domain.Entities;
using Xunit;

namespace Kvit.Api.Tests.AdminStats
{
    public class AdminStatsErrorTests(AdminStatsApp app) : AdminStatsTestBase(app)
    {
        [Fact]
        public async Task Stats_FromAfterTo_Answers400RangeInvalid()
        {
            HttpClient client = await AdminClientAsync();

            HttpResponseMessage response = await AdminStatsRequests.GetAsync(client, from: "2026-11-03", to: "2026-11-01");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, "ADMIN_STATS_RANGE_INVALID");
        }

        [Fact]
        public async Task Stats_ToAfterTodayInSkopje_Answers400FutureDate()
        {
            HttpClient client = await AdminClientAsync();

            HttpResponseMessage response = await AdminStatsRequests.GetAsync(client, from: "2026-11-01", to: "2026-11-05");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, "ADMIN_STATS_FUTURE_DATE");
        }

        [Fact]
        public async Task Stats_FromAfterTodayAndToOmitted_Answers400FutureDate()
        {
            HttpClient client = await AdminClientAsync();

            HttpResponseMessage response = await AdminStatsRequests.GetAsync(client, from: "2026-11-05");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, "ADMIN_STATS_FUTURE_DATE");
        }

        [Fact]
        public async Task Stats_ToIsTheDayAfterTodayInSkopjeWhenSkopjeIsAlreadyAheadOfUtc_Answers400FutureDate()
        {
            _app.Clock.SetTo(Moment.At("2026-11-04T23:30:00Z"));
            HttpClient client = await AdminClientAsync();

            HttpResponseMessage allowed = await AdminStatsRequests.GetAsync(client, from: "2026-11-05", to: "2026-11-05");
            HttpResponseMessage refused = await AdminStatsRequests.GetAsync(client, from: "2026-11-05", to: "2026-11-06");

            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
            await ProblemResponse.AssertAsync(refused, HttpStatusCode.BadRequest, "ADMIN_STATS_FUTURE_DATE");
        }

        [Theory]
        [InlineData("hour")]
        [InlineData("quarter")]
        [InlineData("days")]
        public async Task Stats_UnknownGrouping_Answers400GroupingInvalid(string by)
        {
            HttpClient client = await AdminClientAsync();

            HttpResponseMessage response = await AdminStatsRequests.GetAsync(client, from: "2026-11-01", to: "2026-11-04", by: by);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, "ADMIN_STATS_GROUPING_INVALID");
        }

        [Fact]
        public async Task Stats_DayGroupingOf93Days_Answers400DayRangeTooLong()
        {
            HttpClient client = await AdminClientAsync();

            HttpResponseMessage response = await AdminStatsRequests.GetAsync(client, from: "2026-08-04", to: "2026-11-04", by: "day");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, "ADMIN_STATS_DAY_RANGE_TOO_LONG");
        }

        [Fact]
        public async Task Stats_GroupingOmittedAndMoreThan92Days_Answers400DayRangeTooLong()
        {
            HttpClient client = await AdminClientAsync();

            HttpResponseMessage response = await AdminStatsRequests.GetAsync(client, from: "2026-01-01", to: "2026-11-04");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, "ADMIN_STATS_DAY_RANGE_TOO_LONG");
        }

        [Fact]
        public async Task Stats_DayGroupingOfExactly92Days_Answers200With92Buckets()
        {
            JsonElement stats = await StatsAsync(from: "2026-08-05", to: "2026-11-04", by: "day");

            List<string> buckets = StatsJson.Buckets(stats);
            Assert.Equal(92, buckets.Count);
            Assert.Equal("2026-08-05", buckets[0]);
            Assert.Equal("2026-11-04", buckets[^1]);
        }

        [Theory]
        [InlineData("week")]
        [InlineData("month")]
        [InlineData("year")]
        public async Task Stats_LongRangeWithAnotherGrouping_Answers200(string by)
        {
            HttpClient client = await AdminClientAsync();

            HttpResponseMessage response = await AdminStatsRequests.GetAsync(client, from: "2026-01-01", to: "2026-11-04", by: by);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Stats_FromOmittedAndTheEarliestEventIsMoreThan92DaysAgo_Answers400DayRangeTooLong()
        {
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-01-01T10:00:00Z")));
            HttpClient client = await AdminClientAsync();

            HttpResponseMessage response = await AdminStatsRequests.GetAsync(client);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, "ADMIN_STATS_DAY_RANGE_TOO_LONG");
        }

        [Fact]
        public async Task Stats_FromOmittedAndTheEarliestEventIsMoreThan92DaysAgoButGroupedByMonth_Answers200FromThatDay()
        {
            Guid person = await _app.AddPersonAsync();
            await _app.AddEventsAsync(UsageEvent.SignedUp(person, SignUpMethod.Email, Moment.At("2026-01-01T10:00:00Z")));

            JsonElement stats = await StatsAsync(by: "month");

            Assert.Equal("2026-01-01", StatsJson.Text(stats, "from"));
            Assert.Equal(11, StatsJson.Buckets(stats).Count);
        }

        [Theory]
        [InlineData("from", "not-a-date")]
        [InlineData("from", "2026-13-01")]
        [InlineData("from", "2026-02-30")]
        [InlineData("to", "not-a-date")]
        [InlineData("to", "2026-13-01")]
        public async Task Stats_DateThatCannotBeRead_Answers400(string name, string value)
        {
            HttpClient client = await AdminClientAsync();

            HttpResponseMessage response = await client.GetAsync($"{AdminStatsRequests.Path}?{name}={value}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
