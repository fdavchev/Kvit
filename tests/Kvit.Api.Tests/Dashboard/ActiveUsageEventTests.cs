using System.Globalization;
using System.Net;
using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Dashboard
{
    public class ActiveUsageEventTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

        [Fact]
        public async Task Active_SignedIn_Answers204WithAnEmptyBodyAndWritesOneActiveRow()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await DashboardRequests.ActiveAsync(user.Client);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Equal(1, await ActiveRows.CountOfAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Active_FirstCall_WritesTheRowWithNoDetailTheClockTimeAndTheUtcDay()
        {
            SignedInUser user = await _app.SignUpAsync();

            await DashboardRequests.ActiveAsync(user.Client);

            Assert.Equal(SettlementRows.UtcTextOf(_app.Clock.GetUtcNow()), await ActiveRows.ValueOfAsync(_app, user.UserId, "occurred_at AT TIME ZONE 'UTC'"));
            Assert.Equal(_app.Clock.UtcToday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), await ActiveRows.ValueOfAsync(_app, user.UserId, "occurred_on"));
            Assert.Null(await ActiveRows.ValueOfAsync(_app, user.UserId, "detail"));
        }

        [Fact]
        public async Task Active_SecondCallTheSameUtcDay_AnswersNoContentAndWritesNoSecondRow()
        {
            SignedInUser user = await _app.SignUpAsync();
            await DashboardRequests.ActiveAsync(user.Client);
            _app.Clock.Advance(TimeSpan.FromMinutes(5));

            HttpResponseMessage response = await DashboardRequests.ActiveAsync(user.Client);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Equal(1, await ActiveRows.CountOfAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Active_CallsAtTheStartAndAtTheLastSecondOfTheSameUtcDay_WriteOneRow()
        {
            SignedInUser user = await _app.SignUpAsync();
            DateTimeOffset startOfToday = StartOfUtcDay(_app.Clock.UtcToday);
            DashboardSteps.AdvanceTo(_app.Clock, startOfToday.AddDays(1));
            await DashboardRequests.ActiveAsync(user.Client);
            DashboardSteps.AdvanceTo(_app.Clock, startOfToday.AddDays(2) - OneSecond);

            await DashboardRequests.ActiveAsync(user.Client);

            Assert.Equal(1, await ActiveRows.CountOfAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Active_CallOneSecondAcrossUtcMidnight_WritesASecondRowForTheNewDay()
        {
            SignedInUser user = await _app.SignUpAsync();
            DateTimeOffset startOfToday = StartOfUtcDay(_app.Clock.UtcToday);
            DashboardSteps.AdvanceTo(_app.Clock, startOfToday.AddDays(2) - OneSecond);
            await DashboardRequests.ActiveAsync(user.Client);
            _app.Clock.Advance(OneSecond);

            HttpResponseMessage response = await DashboardRequests.ActiveAsync(user.Client);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Equal(2, await ActiveRows.CountOfAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Active_OnThreeDaysInARow_WritesOneRowPerDayEachWithItsOwnDay()
        {
            SignedInUser user = await _app.SignUpAsync();
            DateOnly firstDay = _app.Clock.UtcToday;

            await DashboardRequests.ActiveAsync(user.Client);
            _app.Clock.Advance(TimeSpan.FromDays(1));
            await DashboardRequests.ActiveAsync(user.Client);
            _app.Clock.Advance(TimeSpan.FromDays(1));
            await DashboardRequests.ActiveAsync(user.Client);

            Assert.Equal(
                [firstDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), firstDay.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), firstDay.AddDays(2).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)],
                await ActiveRows.DaysOfAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Active_TwoUsersTheSameDay_EachGetTheirOwnRow()
        {
            SignedInUser first = await _app.SignUpAsync();
            SignedInUser second = await _app.SignUpAsync();

            await DashboardRequests.ActiveAsync(first.Client);
            await DashboardRequests.ActiveAsync(second.Client);

            Assert.Equal(1, await ActiveRows.CountOfAsync(_app, first.UserId));
            Assert.Equal(1, await ActiveRows.CountOfAsync(_app, second.UserId));
        }

        [Fact]
        public async Task Active_WritesNoUsageEventOfAnyOtherType()
        {
            SignedInUser user = await _app.SignUpAsync();
            int before = await ClosingRows.CountUsageEventsOfAsync(_app, user.UserId);

            await DashboardRequests.ActiveAsync(user.Client);
            await DashboardRequests.ActiveAsync(user.Client);

            Assert.Equal(before + 1, await ClosingRows.CountUsageEventsOfAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Active_ReadingTheDashboardAfterwards_StillLeavesExactlyOneRow()
        {
            SignedInUser user = await _app.SignUpAsync();
            await DashboardRequests.ActiveAsync(user.Client);

            await DashboardRequests.ReadAsync(user);

            Assert.Equal(1, await ActiveRows.CountOfAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Active_Anonymous_Answers401WritesNothingWhileASignedInUserGets204()
        {
            SignedInUser user = await _app.SignUpAsync();
            HttpClient anonymous = _app.CreateClient();
            int before = await ActiveRows.CountOfEveryoneAsync(_app);

            HttpResponseMessage signedIn = await DashboardRequests.ActiveAsync(user.Client);
            HttpResponseMessage response = await DashboardRequests.ActiveAsync(anonymous);

            Assert.Equal(HttpStatusCode.NoContent, signedIn.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(before + 1, await ActiveRows.CountOfEveryoneAsync(_app));
        }

        private static DateTimeOffset StartOfUtcDay(DateOnly day)
        {
            return new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        }
    }
}
