using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public class PurgeAccessTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task LogIn_AfterThePurge_AnswersInvalidCredentials()
        {
            SignedInUser user = await _app.SignUpAsync();
            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            HttpResponseMessage response = await AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, user.Form.Password);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Unauthorized, ResultCodes.AUTH_INVALID_CREDENTIALS);
            Assert.Null(AuthCookie.Find(response));
        }

        [Fact]
        public async Task LogIn_AfterThePurge_DoesNotUndoThePurge()
        {
            SignedInUser user = await _app.SignUpAsync();
            DateTimeOffset now = await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            await AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, user.Form.Password);

            Assert.Equal(now, (await DeletionRows.ReadAsync(_app, user.UserId)).PurgedAt);
        }

        [Fact]
        public async Task GoogleLogIn_AfterThePurge_AnswersNoAccountLinked()
        {
            GoogleAccount account = await DeletionSteps.SignUpWithGoogleAsync(_app);
            await DeletionSteps.RequestAndPurgeAsync(_app, account.Client, account.UserId, DeletionRequests.KeepName);

            HttpResponseMessage response = await AuthRequests.GoogleLogInAsync(_app.CreateClient(), _app.AddGoogleToken(account.Identity));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.AUTH_GOOGLE_NO_ACCOUNT);
        }

        [Fact]
        public async Task GoogleSignUp_SameGoogleAccountAfterThePurge_CreatesANewAccount()
        {
            GoogleAccount account = await DeletionSteps.SignUpWithGoogleAsync(_app);
            await DeletionSteps.RequestAndPurgeAsync(_app, account.Client, account.UserId, DeletionRequests.KeepName);

            HttpResponseMessage response = await AuthRequests.GoogleSignUpAsync(_app.CreateClient(), _app.AddGoogleToken(account.Identity));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Guid newUserId = (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid();
            Assert.NotEqual(account.UserId, newUserId);
        }

        [Fact]
        public async Task Register_SameEmailAfterThePurge_CreatesANewAccountWithANewId()
        {
            SignedInUser user = await _app.SignUpAsync();
            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            HttpResponseMessage response = await AuthRequests.RegisterAsync(_app.CreateClient(), user.Form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Guid newUserId = (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid();
            Assert.NotEqual(user.UserId, newUserId);
        }

        [Fact]
        public async Task Register_SameEmailAfterThePurge_LeavesTheTombstoneAlone()
        {
            SignedInUser user = await _app.SignUpAsync("Zorica");
            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);
            string tombstoneBefore = await DeletionRows.SnapshotOfUserRowAsync(_app, user.UserId);

            await AuthRequests.RegisterAsync(_app.CreateClient(), user.Form);

            Assert.Equal(tombstoneBefore, await DeletionRows.SnapshotOfUserRowAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Purge_OldSessionOfThePurgedAccount_IsRefused()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            HttpClient secondDevice = await _app.CreateLoggedInClientAsync(group.Member.Form.Email, group.Member.Form.Password);
            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.KeepName);
            _app.Clock.Advance(TimeSpan.FromSeconds(1));

            HttpResponseMessage response = await GroupRequests.ListMembersAsync(secondDevice, group.GroupId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
