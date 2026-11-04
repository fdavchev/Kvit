using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Contracts.Auth;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class IsAdminTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string FreeName = "Marko";

        [Fact]
        public async Task Me_NormalUser_AnswersIsAdminFalse()
        {
            HttpClient client = await _app.CreateRegisteredClientAsync(RegistrationForm.Valid());

            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);

            Assert.False(await IsAdminOfAsync(response));
        }

        [Fact]
        public async Task Me_AdminLoggedIn_AnswersIsAdminTrue()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            await _app.MakeAdminAsync(form.Email);
            HttpClient client = await _app.CreateLoggedInClientAsync(form.Email, form.Password);

            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);

            Assert.True(await IsAdminOfAsync(response));
        }

        [Fact]
        public async Task Me_SignedInBeforeBeingMadeAdmin_AnswersTrueOnTheNextCall()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = await _app.CreateRegisteredClientAsync(form);
            Assert.False(await IsAdminOfAsync(await AuthRequests.GetMeAsync(client)));

            await _app.MakeAdminAsync(form.Email);

            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(await IsAdminOfAsync(response));
        }

        [Fact]
        public async Task Me_AdminWhoIsRevokedWhileSignedIn_AnswersFalseOnTheNextCall()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            await _app.MakeAdminAsync(form.Email);
            HttpClient client = await _app.CreateLoggedInClientAsync(form.Email, form.Password);
            Assert.True(await IsAdminOfAsync(await AuthRequests.GetMeAsync(client)));

            await _app.RevokeAdminAsync(form.Email);

            HttpResponseMessage response = await AuthRequests.GetMeAsync(client);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(await IsAdminOfAsync(response));
        }

        [Fact]
        public async Task Me_OneAdmin_AnotherSignedInUserStillAnswersFalse()
        {
            RegistrationForm admin = RegistrationForm.Valid();
            HttpClient adminClient = await _app.CreateRegisteredClientAsync(admin);
            HttpClient otherClient = await _app.CreateRegisteredClientAsync(RegistrationForm.Valid());

            await _app.MakeAdminAsync(admin.Email);

            Assert.True(await IsAdminOfAsync(await AuthRequests.GetMeAsync(adminClient)));
            Assert.False(await IsAdminOfAsync(await AuthRequests.GetMeAsync(otherClient)));
        }

        [Fact]
        public async Task Me_Guest_AnswersIsAdminFalse()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);

            HttpResponseMessage response = await AuthRequests.GetMeAsync(guest.Client);

            Assert.False(await IsAdminOfAsync(response));
        }

        [Fact]
        public async Task Register_NewUser_AnswersIsAdminFalse()
        {
            HttpResponseMessage response = await AuthRequests.RegisterAsync(_app.CreateClient(), RegistrationForm.Valid());

            Assert.False(await IsAdminOfAsync(response));
        }

        [Fact]
        public async Task LogIn_NormalUser_AnswersIsAdminFalse()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);

            HttpResponseMessage response = await AuthRequests.LogInAsync(_app.CreateClient(), form.Email, form.Password);

            Assert.False(await IsAdminOfAsync(response));
        }

        [Fact]
        public async Task LogIn_Admin_AnswersIsAdminTrue()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);
            await _app.MakeAdminAsync(form.Email);

            HttpResponseMessage response = await AuthRequests.LogInAsync(_app.CreateClient(), form.Email, form.Password);

            Assert.True(await IsAdminOfAsync(response));
        }

        [Fact]
        public async Task GoogleLogIn_Admin_AnswersIsAdminTrue()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            await _app.CreateGoogleSignedUpClientAsync(identity);
            await _app.MakeAdminAsync(identity.Email);

            HttpResponseMessage response = await AuthRequests.GoogleLogInAsync(_app.CreateClient(), _app.AddGoogleToken(identity));

            Assert.True(await IsAdminOfAsync(response));
        }

        [Fact]
        public async Task GoogleSignUp_NewUser_AnswersIsAdminFalse()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();

            HttpResponseMessage response = await AuthRequests.GoogleSignUpAsync(_app.CreateClient(), _app.AddGoogleToken(identity));

            Assert.False(await IsAdminOfAsync(response));
        }

        [Fact]
        public async Task UpgradeGuest_Guest_AnswersIsAdminFalse()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);

            HttpResponseMessage response = await AuthRequests.UpgradeGuestAsync(guest.Client, RegistrationForm.UniqueEmail(), "Guest-Pass-2026");

            Assert.False(await IsAdminOfAsync(response));
        }

        private static async Task<bool> IsAdminOfAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);

            return body.GetProperty("isAdmin").GetBoolean();
        }
    }
}
