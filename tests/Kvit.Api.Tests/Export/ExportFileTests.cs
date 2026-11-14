using System.Globalization;
using System.Net;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.Export
{
    public class ExportFileTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Export_SignedInAccount_AnswersJson()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await ExportRequests.GetAsync(user.Client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        [Theory]
        [InlineData("2026-11-04T09:30:00Z", "2026-11-04")]
        [InlineData("2026-11-04T23:30:00Z", "2026-11-04")]
        [InlineData("2026-11-05T00:00:00Z", "2026-11-05")]
        [InlineData("2026-12-31T23:59:59Z", "2026-12-31")]
        public async Task Export_AnswersAnAttachmentNamedWithTheUtcDateOfTheAppClock(string clockTime, string expectedDate)
        {
            SignedInUser user = await _app.SignUpAsync();
            _app.Clock.SetTo(DateTimeOffset.Parse(clockTime, CultureInfo.InvariantCulture));

            HttpResponseMessage response = await ExportRequests.GetAsync(user.Client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(ExportRequests.FileNameOn(DateOnly.Parse(expectedDate, CultureInfo.InvariantCulture)), ExportRequests.FileNameOf(response));
        }

        [Fact]
        public async Task Export_Guest_AnswersAnAttachmentWithTheSameFileNamePattern()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");
            _app.Clock.SetTo(ExpensesApp.ClockStart);

            HttpResponseMessage response = await ExportRequests.GetAsync(guest.Client);

            Assert.Equal(ExportRequests.FileNameOn(_app.Clock.UtcToday), ExportRequests.FileNameOf(response));
        }

        [Fact]
        public async Task Export_ExportedAt_IsTheAppClockTime()
        {
            SignedInUser user = await _app.SignUpAsync();
            DateTimeOffset exportTime = new(2026, 11, 4, 9, 30, 15, TimeSpan.Zero);
            _app.Clock.SetTo(exportTime);

            ExportBody body = await ExportRequests.GetBodyAsync(user.Client);

            Assert.Equal(exportTime, body.Root.GetProperty("exportedAt").GetDateTimeOffset());
        }
    }
}
