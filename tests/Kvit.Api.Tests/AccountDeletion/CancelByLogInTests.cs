using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public class CancelByLogInTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task LogIn_PendingAccount_Answers200WithDeletionCancelledTrue()
        {
            SignedInUser user = await _app.SignUpAsync();
            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.KeepName);

            HttpResponseMessage response = await AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, user.Form.Password);

            JsonElement body = await DeletionSteps.ReadLogInAsync(response);
            Assert.True(DeletionSteps.DeletionCancelledOf(body));
        }

        [Theory]
        [InlineData("KeepName")]
        [InlineData("FormerMember")]
        public async Task LogIn_PendingAccount_ClearsTheRequestAndTheChoice(string nameChoice)
        {
            SignedInUser user = await _app.SignUpAsync();
            await DeletionSteps.RequestAsync(user.Client, nameChoice);

            await DeletionSteps.ReadLogInAsync(await AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, user.Form.Password));

            Assert.Equal(StoredDeletion.None, await DeletionRows.ReadAsync(_app, user.UserId));
        }

        [Fact]
        public async Task LogIn_PendingAccount_AnswersTheUsualAccountFields()
        {
            SignedInUser user = await _app.SignUpAsync("Zorica");
            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.KeepName);

            HttpResponseMessage response = await AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, user.Form.Password);

            JsonElement body = await DeletionSteps.ReadLogInAsync(response);
            string[] propertyNames = [.. body.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)];
            Assert.Equal(MeFields.SortedAccountFieldNames, propertyNames);
            Assert.Equal(user.UserId, body.GetProperty("id").GetGuid());
            Assert.Equal("Zorica", body.GetProperty("displayName").GetString());
        }

        [Fact]
        public async Task LogIn_PendingAccount_GivesANewSessionThatWorks()
        {
            SignedInUser user = await _app.SignUpAsync();
            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.KeepName);
            HttpClient newDevice = _app.CreateClient();
            await DeletionSteps.ReadLogInAsync(await AuthRequests.LogInAsync(newDevice, user.Form.Email, user.Form.Password));

            HttpResponseMessage me = await AuthRequests.GetMeAsync(newDevice);

            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
            Assert.False(DeletionSteps.DeletionCancelledOf(await AuthRequests.ReadJsonAsync(me)));
        }

        [Fact]
        public async Task LogIn_PendingAccountTwice_AnswersDeletionCancelledTrueOnlyTheFirstTime()
        {
            SignedInUser user = await _app.SignUpAsync();
            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.KeepName);

            JsonElement first = await DeletionSteps.ReadLogInAsync(await AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, user.Form.Password));
            JsonElement second = await DeletionSteps.ReadLogInAsync(await AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, user.Form.Password));

            Assert.True(DeletionSteps.DeletionCancelledOf(first));
            Assert.False(DeletionSteps.DeletionCancelledOf(second));
        }

        [Fact]
        public async Task LogIn_AccountWithNoDeletion_AnswersDeletionCancelledFalse()
        {
            SignedInUser user = await _app.SignUpAsync();

            JsonElement body = await DeletionSteps.ReadLogInAsync(await AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, user.Form.Password));

            Assert.False(DeletionSteps.DeletionCancelledOf(body));
        }

        [Fact]
        public async Task LogIn_WrongPassword_DoesNotCancelTheDeletion()
        {
            SignedInUser user = await _app.SignUpAsync();
            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.FormerMember);
            StoredDeletion before = await DeletionRows.ReadAsync(_app, user.UserId);

            HttpResponseMessage response = await AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, AuthRequests.WrongPassword);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Unauthorized, ResultCodes.AUTH_INVALID_CREDENTIALS);
            Assert.Equal(before, await DeletionRows.ReadAsync(_app, user.UserId));
            Assert.NotNull(before.RequestedAt);
        }

        [Fact]
        public async Task LogIn_WrongPasswordThenTheRightOne_CancelsOnTheRightOne()
        {
            SignedInUser user = await _app.SignUpAsync();
            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.KeepName);
            await AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, AuthRequests.WrongPassword);

            JsonElement body = await DeletionSteps.ReadLogInAsync(await AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, user.Form.Password));

            Assert.True(DeletionSteps.DeletionCancelledOf(body));
            Assert.Equal(StoredDeletion.None, await DeletionRows.ReadAsync(_app, user.UserId));
        }

        [Fact]
        public async Task LogIn_AfterTheCancel_TheSessionsFromBeforeTheRequestStayDead()
        {
            SignedInUser user = await _app.SignUpAsync();
            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.KeepName);
            await DeletionSteps.ReadLogInAsync(await AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, user.Form.Password));

            HttpResponseMessage me = await AuthRequests.GetMeAsync(user.Client);

            Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        }

        [Fact]
        public async Task LogIn_PendingAccountTwentyNineDaysLater_StillCancels()
        {
            SignedInUser user = await _app.SignUpAsync();
            _app.Clock.SetTo(ExpensesApp.ClockStart);
            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.KeepName);
            _app.Clock.SetTo(ExpensesApp.ClockStart + TimeSpan.FromDays(29));

            JsonElement body = await DeletionSteps.ReadLogInAsync(await AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, user.Form.Password));

            Assert.True(DeletionSteps.DeletionCancelledOf(body));
            Assert.Equal(StoredDeletion.None, await DeletionRows.ReadAsync(_app, user.UserId));
        }

        [Fact]
        public async Task LogIn_PendingAccountPastThirtyDaysButNotYetPurged_StillCancels()
        {
            SignedInUser user = await _app.SignUpAsync();
            _app.Clock.SetTo(ExpensesApp.ClockStart);
            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.KeepName);
            _app.Clock.SetTo(ExpensesApp.ClockStart + TimeSpan.FromDays(31));

            JsonElement body = await DeletionSteps.ReadLogInAsync(await AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, user.Form.Password));

            Assert.True(DeletionSteps.DeletionCancelledOf(body));
            Assert.Equal(StoredDeletion.None, await DeletionRows.ReadAsync(_app, user.UserId));
        }

        [Fact]
        public async Task LogIn_AfterTheCancel_ThePersonIsStillInTheirGroupWithTheSameRow()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await DeletionSteps.RequestAsync(group.Member.Client, DeletionRequests.FormerMember);

            await DeletionSteps.ReadLogInAsync(await AuthRequests.LogInAsync(_app.CreateClient(), group.Member.Form.Email, group.Member.Form.Password));

            Assert.Contains(group.MemberRowId, await GroupRequests.MemberIdsAsync(group.Owner.Client, group.GroupId));
        }

        [Fact]
        public async Task Register_NewAccount_AnswersDeletionCancelledFalse()
        {
            HttpResponseMessage response = await AuthRequests.RegisterAsync(_app.CreateClient(), RegistrationForm.Valid());

            Assert.False(DeletionSteps.DeletionCancelledOf(await DeletionSteps.ReadLogInAsync(response)));
        }

        [Fact]
        public async Task GoogleSignUp_NewAccount_AnswersDeletionCancelledFalse()
        {
            HttpResponseMessage response = await AuthRequests.GoogleSignUpAsync(_app.CreateClient(), _app.AddGoogleToken(GoogleIdentities.Verified()));

            Assert.False(DeletionSteps.DeletionCancelledOf(await DeletionSteps.ReadLogInAsync(response)));
        }

        [Fact]
        public async Task UpgradeGuest_ValidEmailAndPassword_AnswersDeletionCancelledFalse()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");

            HttpResponseMessage response = await AuthRequests.UpgradeGuestAsync(guest.Client, RegistrationForm.UniqueEmail(), "Guest-Pass-2026");

            Assert.False(DeletionSteps.DeletionCancelledOf(await DeletionSteps.ReadLogInAsync(response)));
        }

        [Fact]
        public async Task GoogleUpgradeGuest_ValidToken_AnswersDeletionCancelledFalse()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");

            HttpResponseMessage response = await AuthRequests.GoogleUpgradeGuestAsync(guest.Client, _app.AddGoogleToken(GoogleIdentities.Verified()));

            Assert.False(DeletionSteps.DeletionCancelledOf(await DeletionSteps.ReadLogInAsync(response)));
        }

        [Fact]
        public async Task GetMe_NormalAccount_AnswersDeletionCancelledFalse()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage me = await AuthRequests.GetMeAsync(user.Client);

            Assert.False(DeletionSteps.DeletionCancelledOf(await DeletionSteps.ReadLogInAsync(me)));
        }

        [Fact]
        public async Task GetMe_WithTheOldCookieWhileThePendingDeletion_Answers401()
        {
            SignedInUser user = await _app.SignUpAsync();
            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.KeepName);

            HttpResponseMessage me = await AuthRequests.GetMeAsync(user.Client);

            Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        }
    }
}
