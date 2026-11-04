using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Terms
{
    public class TermsRecordedTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string FreeName = "Marko";
        private const string NewName = "Cvetan";
        private const string NewPassword = "Guest-Pass-2026";
        private static readonly DateTimeOffset SignUpTime = new(2026, 11, 4, 9, 30, 15, TimeSpan.Zero);
        private static readonly DateTimeOffset LaterTime = SignUpTime.AddHours(2);
        private static readonly DateTimeOffset OldAcceptedTime = new(2025, 1, 1, 8, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task Register_ValidData_StoresTheCurrentVersionAndTheTimeOfTheAppClockLikeCreatedAt()
        {
            _app.Clock.SetTo(SignUpTime);
            RegistrationForm form = RegistrationForm.Valid();

            Guid userId = await _app.CreateAccountAsync(form);

            Assert.Equal(new StoredTerms(TermsRequests.CurrentVersion, SignUpTime), await TermsRows.ReadAsync(_app, userId));
            Assert.Equal(SignUpTime, (await _app.FindUserAsync(form.Email)).CreatedAt);
        }

        [Fact]
        public async Task Register_EmailAlreadyTaken_LeavesTheTermsOfTheFirstAccountUnchanged()
        {
            RegistrationForm form = RegistrationForm.Valid();
            Guid userId = await _app.CreateAccountAsync(form);
            await TermsRows.SetAsync(_app, userId, TermsRequests.OlderVersion, OldAcceptedTime);
            _app.Clock.SetTo(LaterTime);

            HttpResponseMessage response = await AuthRequests.RegisterAsync(_app.CreateClient(), form);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.AUTH_EMAIL_TAKEN);
            Assert.Equal(new StoredTerms(TermsRequests.OlderVersion, OldAcceptedTime), await TermsRows.ReadAsync(_app, userId));
        }

        [Fact]
        public async Task GoogleSignUp_ValidData_StoresTheCurrentVersionAndTheTimeOfTheAppClock()
        {
            _app.Clock.SetTo(SignUpTime);
            GoogleIdentity identity = GoogleIdentities.Verified();

            await _app.CreateGoogleSignedUpClientAsync(identity);

            Guid userId = (await _app.FindUserAsync(identity.Email)).Id;
            Assert.Equal(new StoredTerms(TermsRequests.CurrentVersion, SignUpTime), await TermsRows.ReadAsync(_app, userId));
        }

        [Fact]
        public async Task JoinAsGuest_TappingAFreeName_StoresTheCurrentVersionAndTheTimeOfTheAppClock()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync(FreeName);
            _app.Clock.SetTo(SignUpTime);

            GuestMember guest = await GuestSteps.TapFreeNameAsync(_app, group.GroupId, group.PlainRowIds[0]);

            Assert.Equal(new StoredTerms(TermsRequests.CurrentVersion, SignUpTime), await TermsRows.ReadAsync(_app, guest.UserId));
        }

        [Fact]
        public async Task JoinAsGuest_WithANewName_StoresTheCurrentVersionAndTheTimeOfTheAppClock()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync(FreeName);
            _app.Clock.SetTo(SignUpTime);

            GuestMember guest = await GuestSteps.JoinWithNewNameAsync(_app, group.GroupId, NewName);

            Assert.Equal(new StoredTerms(TermsRequests.CurrentVersion, SignUpTime), await TermsRows.ReadAsync(_app, guest.UserId));
        }

        [Fact]
        public async Task UpgradeGuest_ValidEmailAndPassword_SetsTheCurrentVersionAndTheTimeOfTheUpgrade()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);
            await TermsRows.SetAsync(_app, guest.UserId, TermsRequests.OlderVersion, OldAcceptedTime);
            _app.Clock.SetTo(LaterTime);

            HttpResponseMessage response = await AuthRequests.UpgradeGuestAsync(guest.Client, RegistrationForm.UniqueEmail(), NewPassword);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new StoredTerms(TermsRequests.CurrentVersion, LaterTime), await TermsRows.ReadAsync(_app, guest.UserId));
        }

        [Fact]
        public async Task UpgradeGuest_EmailOfAnotherAccount_LeavesTheTermsOfTheGuestUnchanged()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);
            await TermsRows.SetAsync(_app, guest.UserId, TermsRequests.OlderVersion, OldAcceptedTime);
            RegistrationForm other = RegistrationForm.Valid();
            await _app.CreateAccountAsync(other);
            _app.Clock.SetTo(LaterTime);

            HttpResponseMessage response = await AuthRequests.UpgradeGuestAsync(guest.Client, other.Email, NewPassword);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.AUTH_EMAIL_TAKEN);
            Assert.Equal(new StoredTerms(TermsRequests.OlderVersion, OldAcceptedTime), await TermsRows.ReadAsync(_app, guest.UserId));
        }

        [Fact]
        public async Task GoogleUpgradeGuest_ValidToken_SetsTheCurrentVersionAndTheTimeOfTheUpgrade()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);
            await TermsRows.SetAsync(_app, guest.UserId, TermsRequests.OlderVersion, OldAcceptedTime);
            _app.Clock.SetTo(LaterTime);

            HttpResponseMessage response = await AuthRequests.GoogleUpgradeGuestAsync(guest.Client, _app.AddGoogleToken(GoogleIdentities.Verified()));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new StoredTerms(TermsRequests.CurrentVersion, LaterTime), await TermsRows.ReadAsync(_app, guest.UserId));
        }

        [Fact]
        public async Task GoogleUpgradeGuest_EmailOfAnotherAccount_LeavesTheTermsOfTheGuestUnchanged()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);
            await TermsRows.SetAsync(_app, guest.UserId, TermsRequests.OlderVersion, OldAcceptedTime);
            RegistrationForm other = RegistrationForm.Valid();
            await _app.CreateAccountAsync(other);
            GoogleIdentity identity = GoogleIdentities.Verified() with { Email = other.Email };
            _app.Clock.SetTo(LaterTime);

            HttpResponseMessage response = await AuthRequests.GoogleUpgradeGuestAsync(guest.Client, _app.AddGoogleToken(identity));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.AUTH_GOOGLE_EMAIL_TAKEN);
            Assert.Equal(new StoredTerms(TermsRequests.OlderVersion, OldAcceptedTime), await TermsRows.ReadAsync(_app, guest.UserId));
        }

        [Fact]
        public async Task LogIn_UserWithOlderTerms_KeepsTheOlderVersionAndTime()
        {
            RegistrationForm form = RegistrationForm.Valid();
            Guid userId = await _app.CreateAccountAsync(form);
            await TermsRows.SetAsync(_app, userId, TermsRequests.OlderVersion, OldAcceptedTime);
            _app.Clock.SetTo(LaterTime);

            HttpResponseMessage response = await AuthRequests.LogInAsync(_app.CreateClient(), form.Email, form.Password);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new StoredTerms(TermsRequests.OlderVersion, OldAcceptedTime), await TermsRows.ReadAsync(_app, userId));
        }

        [Fact]
        public async Task LogIn_UserWithNoTerms_StillHasNoTerms()
        {
            RegistrationForm form = RegistrationForm.Valid();
            Guid userId = await _app.CreateAccountAsync(form);
            await TermsRows.ClearAsync(_app, userId);
            _app.Clock.SetTo(LaterTime);

            HttpResponseMessage response = await AuthRequests.LogInAsync(_app.CreateClient(), form.Email, form.Password);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(StoredTerms.None, await TermsRows.ReadAsync(_app, userId));
        }

        [Fact]
        public async Task GoogleLogIn_UserWithOlderTerms_KeepsTheOlderVersionAndTime()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            await _app.CreateGoogleSignedUpClientAsync(identity);
            Guid userId = (await _app.FindUserAsync(identity.Email)).Id;
            await TermsRows.SetAsync(_app, userId, TermsRequests.OlderVersion, OldAcceptedTime);
            _app.Clock.SetTo(LaterTime);

            HttpResponseMessage response = await AuthRequests.GoogleLogInAsync(_app.CreateClient(), _app.AddGoogleToken(identity));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new StoredTerms(TermsRequests.OlderVersion, OldAcceptedTime), await TermsRows.ReadAsync(_app, userId));
        }

        [Fact]
        public async Task Me_UserWithNoTerms_DoesNotWriteTheCurrentVersion()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = await _app.CreateRegisteredClientAsync(form);
            Guid userId = (await _app.FindUserAsync(form.Email)).Id;
            await TermsRows.ClearAsync(_app, userId);

            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(StoredTerms.None, await TermsRows.ReadAsync(_app, userId));
        }
    }
}
