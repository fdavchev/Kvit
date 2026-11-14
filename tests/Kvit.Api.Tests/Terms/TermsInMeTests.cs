using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Contracts.Auth;
using Xunit;

namespace Kvit.Api.Tests.Terms
{
    public class TermsInMeTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string FreeName = "Marko";
        private const string NewPassword = "Guest-Pass-2026";
        private static readonly DateTimeOffset AcceptedTime = new(2025, 1, 1, 8, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task Me_LoggedIn_AnswersTheThreeTermsFieldsRightAfterIsAdminThenDeletionCancelledAndLastIsSuspended()
        {
            HttpClient client = await _app.CreateRegisteredClientAsync(RegistrationForm.Valid());

            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            string[] propertyNames = [.. body.EnumerateObject().Select(property => property.Name)];
            Assert.Equal(["isAdmin", "termsVersion", "termsUpToDate", "termsUpdateIsMaterial", "deletionCancelled", "isSuspended"], propertyNames[^6..]);
        }

        [Fact]
        public async Task Me_NewAccount_AnswersTheCurrentVersionUpToDateAndNotMaterial()
        {
            HttpClient client = await _app.CreateRegisteredClientAsync(RegistrationForm.Valid());

            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);

            Assert.Equal(TermsFields.Current, await TermsRequests.ReadFieldsAsync(response));
        }

        [Fact]
        public async Task Me_UserWhoseTermsColumnsAreNull_AnswersNullVersionNotUpToDateAndNotMaterial()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = await _app.CreateRegisteredClientAsync(form);
            await TermsRows.ClearAsync(_app, (await _app.FindUserAsync(form.Email)).Id);

            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);

            Assert.Equal(TermsFields.NeverAccepted, await TermsRequests.ReadFieldsAsync(response));
        }

        [Fact]
        public async Task Me_UserWithAnOlderVersion_AnswersThatVersionNotUpToDateAndNotMaterialWhileTheCurrentVersionIsNotMaterial()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = await _app.CreateRegisteredClientAsync(form);
            await TermsRows.SetAsync(_app, (await _app.FindUserAsync(form.Email)).Id, TermsRequests.OlderVersion, AcceptedTime);

            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);

            Assert.Equal(TermsFields.Outdated(TermsRequests.OlderVersion), await TermsRequests.ReadFieldsAsync(response));
        }

        [Fact]
        public async Task Me_Guest_AnswersTheCurrentVersionUpToDateAndNotMaterial()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);

            HttpResponseMessage response = await AuthRequests.GetMeAsync(guest.Client);

            Assert.Equal(TermsFields.Current, await TermsRequests.ReadFieldsAsync(response));
        }

        [Fact]
        public async Task Me_GoogleAccount_AnswersTheCurrentVersionUpToDateAndNotMaterial()
        {
            HttpClient client = await _app.CreateGoogleSignedUpClientAsync(GoogleIdentities.Verified());

            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);

            Assert.Equal(TermsFields.Current, await TermsRequests.ReadFieldsAsync(response));
        }

        [Fact]
        public async Task Register_NewAccount_AnswersTheCurrentVersionUpToDateAndNotMaterial()
        {
            HttpResponseMessage response = await AuthRequests.RegisterAsync(_app.CreateClient(), RegistrationForm.Valid());

            Assert.Equal(TermsFields.Current, await TermsRequests.ReadFieldsAsync(response));
        }

        [Fact]
        public async Task GoogleSignUp_NewAccount_AnswersTheCurrentVersionUpToDateAndNotMaterial()
        {
            HttpResponseMessage response = await AuthRequests.GoogleSignUpAsync(_app.CreateClient(), _app.AddGoogleToken(GoogleIdentities.Verified()));

            Assert.Equal(TermsFields.Current, await TermsRequests.ReadFieldsAsync(response));
        }

        [Fact]
        public async Task UpgradeGuest_ValidEmailAndPassword_AnswersTheCurrentVersionUpToDateAndNotMaterial()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);
            await TermsRows.SetAsync(_app, guest.UserId, TermsRequests.OlderVersion, AcceptedTime);

            HttpResponseMessage response = await AuthRequests.UpgradeGuestAsync(guest.Client, RegistrationForm.UniqueEmail(), NewPassword);

            Assert.Equal(TermsFields.Current, await TermsRequests.ReadFieldsAsync(response));
        }

        [Fact]
        public async Task GoogleUpgradeGuest_ValidToken_AnswersTheCurrentVersionUpToDateAndNotMaterial()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);
            await TermsRows.SetAsync(_app, guest.UserId, TermsRequests.OlderVersion, AcceptedTime);

            HttpResponseMessage response = await AuthRequests.GoogleUpgradeGuestAsync(guest.Client, _app.AddGoogleToken(GoogleIdentities.Verified()));

            Assert.Equal(TermsFields.Current, await TermsRequests.ReadFieldsAsync(response));
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData(TermsRequests.OlderVersion, false)]
        [InlineData(TermsRequests.CurrentVersion, true)]
        public async Task LogIn_AndMe_AnswerTheSameTermsFieldsForTheStoredVersion(string? storedVersion, bool expectedUpToDate)
        {
            RegistrationForm form = RegistrationForm.Valid();
            Guid userId = await _app.CreateAccountAsync(form);
            await StoreAsync(userId, storedVersion);
            TermsFields expected = new(storedVersion, expectedUpToDate, false);
            HttpClient client = _app.CreateClient();

            HttpResponseMessage logIn = await AuthRequests.LogInAsync(client, form.Email, form.Password);
            HttpResponseMessage me = await AuthRequests.GetMeAsync(client);

            Assert.Equal(expected, await TermsRequests.ReadFieldsAsync(logIn));
            Assert.Equal(expected, await TermsRequests.ReadFieldsAsync(me));
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData(TermsRequests.OlderVersion, false)]
        [InlineData(TermsRequests.CurrentVersion, true)]
        public async Task GoogleLogIn_AndMe_AnswerTheSameTermsFieldsForTheStoredVersion(string? storedVersion, bool expectedUpToDate)
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            await _app.CreateGoogleSignedUpClientAsync(identity);
            await StoreAsync((await _app.FindUserAsync(identity.Email)).Id, storedVersion);
            TermsFields expected = new(storedVersion, expectedUpToDate, false);
            HttpClient client = _app.CreateClient();

            HttpResponseMessage logIn = await AuthRequests.GoogleLogInAsync(client, _app.AddGoogleToken(identity));
            HttpResponseMessage me = await AuthRequests.GetMeAsync(client);

            Assert.Equal(expected, await TermsRequests.ReadFieldsAsync(logIn));
            Assert.Equal(expected, await TermsRequests.ReadFieldsAsync(me));
        }

        private Task StoreAsync(Guid userId, string? version)
        {
            return version is null
                ? TermsRows.ClearAsync(_app, userId)
                : TermsRows.SetAsync(_app, userId, version, AcceptedTime);
        }
    }
}
