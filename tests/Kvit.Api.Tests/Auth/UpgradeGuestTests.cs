using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Balances;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Auth;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class UpgradeGuestTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string FreeName = "Marko";
        private const string NewPassword = "Guest-Pass-2026";

        [Fact]
        public async Task UpgradeGuest_ValidEmailAndPassword_Answers200WithTheSameIdTheEmailAndIsGuestFalse()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);
            string email = RegistrationForm.UniqueEmail();

            HttpResponseMessage response = await AuthRequests.UpgradeGuestAsync(guest.Client, email, NewPassword);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(["displayName", "email", "hasPassword", "id", "isGuest", "language", "mustChangePassword", "pictureUrl", "timeZone"], [.. body.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)]);
            Assert.Equal(guest.UserId, body.GetProperty("id").GetGuid());
            Assert.Equal(email, body.GetProperty("email").GetString());
            Assert.Equal(FreeName, body.GetProperty("displayName").GetString());
            Assert.False(body.GetProperty("isGuest").GetBoolean());
            Assert.True(body.GetProperty("hasPassword").GetBoolean());
            Assert.False(body.GetProperty("mustChangePassword").GetBoolean());
        }

        [Fact]
        public async Task UpgradeGuest_ValidEmailAndPassword_RefreshesTheLoginSoMeAnswersTheAccountWithTheEmail()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);
            string email = RegistrationForm.UniqueEmail();

            HttpResponseMessage upgraded = await AuthRequests.UpgradeGuestAsync(guest.Client, email, NewPassword);

            Assert.NotNull(AuthCookie.Find(upgraded));
            HttpResponseMessage me = await AuthRequests.GetMeAsync(guest.Client);
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(me);
            Assert.Equal(guest.UserId, body.GetProperty("id").GetGuid());
            Assert.Equal(email, body.GetProperty("email").GetString());
            Assert.False(body.GetProperty("isGuest").GetBoolean());
            Assert.True(body.GetProperty("hasPassword").GetBoolean());
        }

        [Fact]
        public async Task UpgradeGuest_ValidEmailAndPassword_StoresTheEmailAsUserNameOnTheSameRowAndKeepsNameLanguageAndTimeZone()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync(FreeName);
            HttpClient guestClient = _app.CreateClient();
            HttpResponseMessage joined = await GroupRequests.JoinAsGuestAsync(guestClient, await GuestSteps.InviteTokenOfAsync(_app, group.GroupId), claimMemberId: group.PlainRowIds[0], language: "mk", timeZone: "America/New_York");
            Assert.Equal(HttpStatusCode.OK, joined.StatusCode);
            string email = RegistrationForm.UniqueEmail();
            int usersBefore = await _app.CountUsersAsync();

            await AuthRequests.UpgradeGuestAsync(guestClient, $"  {email} ", NewPassword);

            AppUser user = await _app.FindUserAsync(email);
            Assert.Equal(email, user.UserName);
            Assert.Equal(email.ToUpperInvariant(), user.NormalizedEmail);
            Assert.Equal(email.ToUpperInvariant(), user.NormalizedUserName);
            Assert.False(user.IsGuest);
            Assert.NotNull(user.PasswordHash);
            Assert.Equal(FreeName, user.DisplayName);
            Assert.Equal("mk", user.Language);
            Assert.Equal("America/New_York", user.TimeZone);
            Assert.Equal(user.Id.ToString(), await GroupRows.MemberRowValueAsync(_app, group.PlainRowIds[0], "user_id"));
            Assert.Equal(usersBefore, await _app.CountUsersAsync());
        }

        [Fact]
        public async Task UpgradeGuest_ValidEmailAndPassword_KeepsTheMemberRowTheExpensesAndTheBalancesOfTheGroupUnchanged()
        {
            (ExpenseGroup group, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);
            HttpResponseMessage guestExpense = await ExpenseRequests.AddAsync(guest.Client, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, guest.RowId, guest.RowId, group.MemberRowId));
            Assert.Equal(HttpStatusCode.OK, guestExpense.StatusCode);
            await _app.AddExpenseAsync(group.Member, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.MemberRowId, group.MemberRowId, guest.RowId, group.OwnerRowId));
            string groupBefore = await GroupRows.SnapshotAsync(_app, group.GroupId);
            string expensesBefore = await ExpenseRows.SnapshotAsync(_app, group.GroupId);
            string balancesBefore = await BalanceRequests.ReadTextAsync(group.Owner.Client, group.GroupId);

            HttpResponseMessage response = await AuthRequests.UpgradeGuestAsync(guest.Client, RegistrationForm.UniqueEmail(), NewPassword);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(groupBefore, await GroupRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(expensesBefore, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(balancesBefore, await BalanceRequests.ReadTextAsync(group.Owner.Client, group.GroupId));
            Assert.Equal(balancesBefore, await BalanceRequests.ReadTextAsync(guest.Client, group.GroupId));
        }

        [Fact]
        public async Task UpgradeGuest_ValidEmailAndPassword_ShowsTheRowAsNoLongerAGuestInTheMembersList()
        {
            (ExpenseGroup group, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);

            await AuthRequests.UpgradeGuestAsync(guest.Client, RegistrationForm.UniqueEmail(), NewPassword);

            JsonElement row = await GroupRequests.MemberRowAsync(group.Owner.Client, group.GroupId, guest.RowId);
            Assert.False(row.GetProperty("isGuest").GetBoolean());
            Assert.Equal(guest.UserId, row.GetProperty("userId").GetGuid());
        }

        [Fact]
        public async Task UpgradeGuest_ValidEmailAndPassword_ThePersonCanLogOutAndLogInAgainWithTheEmailAndPassword()
        {
            (ExpenseGroup group, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);
            string email = RegistrationForm.UniqueEmail();
            await AuthRequests.UpgradeGuestAsync(guest.Client, email, NewPassword);
            await AuthRequests.LogOutAsync(guest.Client);
            Assert.Equal(HttpStatusCode.Unauthorized, (await AuthRequests.GetMeAsync(guest.Client)).StatusCode);

            HttpResponseMessage loggedIn = await AuthRequests.LogInAsync(guest.Client, email, NewPassword);

            Assert.Equal(HttpStatusCode.OK, loggedIn.StatusCode);
            Assert.Equal(guest.UserId, (await AuthRequests.ReadJsonAsync(loggedIn)).GetProperty("id").GetGuid());
            Assert.Equal(HttpStatusCode.OK, (await GroupRequests.GetAsync(guest.Client, group.GroupId)).StatusCode);
        }

        [Fact]
        public async Task UpgradeGuest_ValidEmailAndPassword_RecordsOneGuestUpgradedEventWithTheEmailMethodAndNoSecondSignUp()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);

            await AuthRequests.UpgradeGuestAsync(guest.Client, RegistrationForm.UniqueEmail(), NewPassword);

            List<UsageEvent> events = await _app.UsageEventsOfAsync(guest.UserId);
            UsageEvent upgraded = Assert.Single(events, usageEvent => usageEvent.Type == UsageEventType.GuestUpgraded);
            Assert.Equal("Email", upgraded.Detail);
            UsageEvent signedUp = Assert.Single(events, usageEvent => usageEvent.Type == UsageEventType.SignedUp);
            Assert.Equal("Guest", signedUp.Detail);
        }

        [Fact]
        public async Task UpgradeGuest_ASecondTimeAfterItWorked_Answers409NotGuestAndKeepsTheFirstEmail()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);
            string firstEmail = RegistrationForm.UniqueEmail();
            await AuthRequests.UpgradeGuestAsync(guest.Client, firstEmail, NewPassword);

            HttpResponseMessage response = await AuthRequests.UpgradeGuestAsync(guest.Client, RegistrationForm.UniqueEmail(), NewPassword);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Conflict, ResultCodes.AUTH_NOT_GUEST);
            Assert.Equal(firstEmail, await GuestUpgradeChecks.UserValueAsync(_app, guest.UserId, "email"));
            Assert.Equal(1, await GroupRows.CountUsageEventsAsync(_app, guest.UserId, "GuestUpgraded"));
        }

        [Fact]
        public async Task UpgradeGuest_EmailOfAnotherAccount_Answers400EmailTakenAndTheGuestStaysAGuest()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);
            RegistrationForm other = RegistrationForm.Valid();
            await _app.CreateAccountAsync(other);

            HttpResponseMessage response = await AuthRequests.UpgradeGuestAsync(guest.Client, other.Email, NewPassword);

            await GuestUpgradeChecks.AssertRefusedAndStillAGuestAsync(_app, response, guest, HttpStatusCode.BadRequest, ResultCodes.AUTH_EMAIL_TAKEN);
            Assert.Equal(1, await _app.CountUsersWithEmailAsync(other.Email));
        }

        [Fact]
        public async Task UpgradeGuest_EmailOfAnotherAccountInOtherCapitals_Answers400EmailTaken()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);
            RegistrationForm other = RegistrationForm.Valid();
            await _app.CreateAccountAsync(other);

            HttpResponseMessage response = await AuthRequests.UpgradeGuestAsync(guest.Client, other.Email.ToUpperInvariant(), NewPassword);

            await GuestUpgradeChecks.AssertRefusedAndStillAGuestAsync(_app, response, guest, HttpStatusCode.BadRequest, ResultCodes.AUTH_EMAIL_TAKEN);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-an-email")]
        [InlineData("ana@")]
        [InlineData("ana petrova@example.com")]
        [InlineData("Ana <ana@example.com>")]
        public async Task UpgradeGuest_InvalidEmail_Answers400EmailInvalidAndTheGuestStaysAGuest(string email)
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);

            HttpResponseMessage response = await AuthRequests.UpgradeGuestAsync(guest.Client, email, NewPassword);

            await GuestUpgradeChecks.AssertRefusedAndStillAGuestAsync(_app, response, guest, HttpStatusCode.BadRequest, ResultCodes.AUTH_EMAIL_INVALID);
        }

        [Theory]
        [InlineData("Short1A")]
        [InlineData("nocapital1")]
        [InlineData("NoNumberHere")]
        [InlineData("")]
        public async Task UpgradeGuest_TooWeakPassword_Answers400PasswordTooWeakAndTheGuestStaysAGuest(string password)
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);

            HttpResponseMessage response = await AuthRequests.UpgradeGuestAsync(guest.Client, RegistrationForm.UniqueEmail(), password);

            await GuestUpgradeChecks.AssertRefusedAndStillAGuestAsync(_app, response, guest, HttpStatusCode.BadRequest, ResultCodes.AUTH_PASSWORD_TOO_WEAK);
        }

        [Fact]
        public async Task UpgradeGuest_NotSignedIn_Answers401AndCreatesNoAccount()
        {
            HttpClient client = _app.CreateClient();
            string email = RegistrationForm.UniqueEmail();

            HttpResponseMessage response = await AuthRequests.UpgradeGuestAsync(client, email, NewPassword);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(0, await _app.CountUsersWithEmailAsync(email));
        }

        [Fact]
        public async Task UpgradeGuest_ByAnAccountWithAPassword_Answers409NotGuestAndKeepsItsEmailAndPassword()
        {
            SignedInUser account = await _app.SignUpAsync();
            string hashBefore = (await GuestUpgradeChecks.UserValueAsync(_app, account.UserId, "password_hash"))!;

            HttpResponseMessage response = await AuthRequests.UpgradeGuestAsync(account.Client, RegistrationForm.UniqueEmail(), NewPassword);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Conflict, ResultCodes.AUTH_NOT_GUEST);
            Assert.Null(AuthCookie.Find(response));
            Assert.Equal(account.Form.Email, await GuestUpgradeChecks.UserValueAsync(_app, account.UserId, "email"));
            Assert.Equal(hashBefore, await GuestUpgradeChecks.UserValueAsync(_app, account.UserId, "password_hash"));
            Assert.Equal(0, await GroupRows.CountUsageEventsAsync(_app, account.UserId, "GuestUpgraded"));
        }

        [Fact]
        public async Task UpgradeGuest_ByAGoogleAccountWithoutAPassword_Answers409NotGuestAndAddsNoPassword()
        {
            HttpClient googleClient = await _app.CreateGoogleSignedUpClientAsync(GoogleIdentities.Verified());
            Guid userId = (await AuthRequests.ReadJsonAsync(await AuthRequests.GetMeAsync(googleClient))).GetProperty("id").GetGuid();

            HttpResponseMessage response = await AuthRequests.UpgradeGuestAsync(googleClient, RegistrationForm.UniqueEmail(), NewPassword);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Conflict, ResultCodes.AUTH_NOT_GUEST);
            Assert.Null(await GuestUpgradeChecks.UserValueAsync(_app, userId, "password_hash"));
        }

        [Fact]
        public async Task UpgradeGuest_ByAnAccountWithAnInvalidEmailAndPassword_Answers409BeforeTheEmailAndPasswordAreChecked()
        {
            SignedInUser account = await _app.SignUpAsync();

            HttpResponseMessage response = await AuthRequests.UpgradeGuestAsync(account.Client, "not-an-email", "weak");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Conflict, ResultCodes.AUTH_NOT_GUEST);
        }
    }
}
