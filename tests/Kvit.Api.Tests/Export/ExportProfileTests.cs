using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.BudgetCurrency;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Terms;
using Kvit.Contracts.Auth;
using Xunit;

namespace Kvit.Api.Tests.Export
{
    public class ExportProfileTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly DateTimeOffset SignUpTime = new(2026, 11, 4, 9, 30, 15, TimeSpan.Zero);
        private static readonly DateTimeOffset OldAcceptedTime = new(2025, 1, 1, 8, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task Export_EmailAccount_ShowsIdNameEmailLanguageTimeZoneAndCreatedAt()
        {
            _app.Clock.SetTo(SignUpTime);
            SignedInUser user = await _app.SignUpAsync("Ana");

            ExportBody body = await ExportRequests.GetBodyAsync(user.Client);

            JsonElement profile = body.Profile;
            Assert.Equal(user.UserId, profile.GetProperty("id").GetGuid());
            Assert.Equal("Ana", profile.GetProperty("displayName").GetString());
            Assert.Equal(user.Form.Email, profile.GetProperty("email").GetString());
            Assert.Equal("en", profile.GetProperty("language").GetString());
            Assert.Equal("Europe/Skopje", profile.GetProperty("timeZone").GetString());
            Assert.Equal(SignUpTime, profile.GetProperty("createdAt").GetDateTimeOffset());
        }

        [Fact]
        public async Task Export_EmailAccount_ShowsIsGuestFalseHasPasswordTrueAndGoogleLinkedFalse()
        {
            SignedInUser user = await _app.SignUpAsync();

            ExportBody body = await ExportRequests.GetBodyAsync(user.Client);

            Assert.False(body.Profile.GetProperty("isGuest").GetBoolean());
            Assert.True(body.Profile.GetProperty("hasPassword").GetBoolean());
            Assert.False(body.Profile.GetProperty("googleLinked").GetBoolean());
        }

        [Fact]
        public async Task Export_EmailAccount_ShowsTheTermsVersionAndTimeAcceptedAtSignUp()
        {
            _app.Clock.SetTo(SignUpTime);
            SignedInUser user = await _app.SignUpAsync();

            ExportBody body = await ExportRequests.GetBodyAsync(user.Client);

            Assert.Equal(TermsRequests.CurrentVersion, body.Profile.GetProperty("termsVersion").GetString());
            Assert.Equal(SignUpTime, body.Profile.GetProperty("termsAcceptedAt").GetDateTimeOffset());
        }

        [Fact]
        public async Task Export_AccountWithOlderTerms_ShowsTheOlderVersionAndItsTime()
        {
            SignedInUser user = await _app.SignUpAsync();
            await TermsRows.SetAsync(_app, user.UserId, TermsRequests.OlderVersion, OldAcceptedTime);

            ExportBody body = await ExportRequests.GetBodyAsync(user.Client);

            Assert.Equal(TermsRequests.OlderVersion, body.Profile.GetProperty("termsVersion").GetString());
            Assert.Equal(OldAcceptedTime, body.Profile.GetProperty("termsAcceptedAt").GetDateTimeOffset());
        }

        [Fact]
        public async Task Export_AccountThatNeverAcceptedTerms_ShowsNullTermsFields()
        {
            SignedInUser user = await _app.SignUpAsync();
            await TermsRows.ClearAsync(_app, user.UserId);

            ExportBody body = await ExportRequests.GetBodyAsync(user.Client);

            Assert.Null(ExportBody.StringOrNull(body.Profile, "termsVersion"));
            Assert.Null(ExportBody.StringOrNull(body.Profile, "termsAcceptedAt"));
        }

        [Fact]
        public async Task Export_AccountWithoutABudgetTotalCurrency_ShowsNull()
        {
            SignedInUser user = await _app.SignUpAsync();

            ExportBody body = await ExportRequests.GetBodyAsync(user.Client);

            Assert.Null(ExportBody.StringOrNull(body.Profile, "budgetTotalCurrency"));
        }

        [Fact]
        public async Task Export_AccountWithABudgetTotalCurrency_ShowsIt()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetCurrencySteps.SetAsync(user, BudgetCurrencyRequests.Eur);

            ExportBody body = await ExportRequests.GetBodyAsync(user.Client);

            Assert.Equal(BudgetCurrencyRequests.Eur, body.Profile.GetProperty("budgetTotalCurrency").GetString());
        }

        [Fact]
        public async Task Export_GoogleOnlyAccount_ShowsGoogleLinkedTrueAndHasPasswordFalse()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            HttpClient client = await _app.CreateGoogleSignedUpClientAsync(identity);

            ExportBody body = await ExportRequests.GetBodyAsync(client);

            Assert.Equal(identity.Email, body.Profile.GetProperty("email").GetString());
            Assert.True(body.Profile.GetProperty("googleLinked").GetBoolean());
            Assert.False(body.Profile.GetProperty("hasPassword").GetBoolean());
            Assert.False(body.Profile.GetProperty("isGuest").GetBoolean());
        }

        [Fact]
        public async Task Export_Guest_ShowsNullEmailAndIsGuestTrue()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");

            ExportBody body = await ExportRequests.GetBodyAsync(guest.Client);

            Assert.Equal(guest.UserId, body.Profile.GetProperty("id").GetGuid());
            Assert.Null(ExportBody.StringOrNull(body.Profile, "email"));
            Assert.True(body.Profile.GetProperty("isGuest").GetBoolean());
        }

        [Fact]
        public async Task Export_Guest_ShowsHasPasswordFalseAndGoogleLinkedFalse()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");

            ExportBody body = await ExportRequests.GetBodyAsync(guest.Client);

            Assert.False(body.Profile.GetProperty("hasPassword").GetBoolean());
            Assert.False(body.Profile.GetProperty("googleLinked").GetBoolean());
        }
    }
}
