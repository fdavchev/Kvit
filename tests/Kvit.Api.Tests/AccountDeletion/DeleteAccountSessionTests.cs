using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Results;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public class DeleteAccountSessionTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Delete_Request_RotatesTheSecurityStamp()
        {
            SignedInUser user = await _app.SignUpAsync();
            string? stampBefore = await DeletionRows.UserValueAsync(_app, user.UserId, "security_stamp");

            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.KeepName);

            Assert.NotNull(stampBefore);
            Assert.NotEqual(stampBefore, await DeletionRows.UserValueAsync(_app, user.UserId, "security_stamp"));
        }

        [Fact]
        public async Task Delete_Request_ClearsTheCallersCookie()
        {
            SignedInUser user = await _app.SignUpAsync();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            HttpResponseMessage response = await DeletionRequests.DeleteAsync(user.Client, DeletionRequests.KeepName);

            SetCookieHeaderValue? cookie = AuthCookie.Find(response);
            Assert.NotNull(cookie);
            DateTimeOffset expiresAt = AuthCookie.ExpiresAt(cookie, now) ?? throw new InvalidOperationException("The clearing kvit_auth cookie has neither Expires nor Max-Age.");
            Assert.True(expiresAt <= now, $"The clearing cookie should already be expired, but it expires at {expiresAt:O}.");
        }

        [Fact]
        public async Task Delete_Request_EndsTheCallersOwnSession()
        {
            SignedInUser user = await _app.SignUpAsync();
            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.KeepName);

            HttpResponseMessage me = await AuthRequests.GetMeAsync(user.Client);

            Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        }

        [Fact]
        public async Task Delete_Request_EndsASecondSessionThatLoggedInEarlier()
        {
            SignedInUser user = await _app.SignUpAsync();
            HttpClient secondDevice = await _app.CreateLoggedInClientAsync(user.Form.Email, user.Form.Password);
            Assert.Equal(HttpStatusCode.OK, (await AuthRequests.GetMeAsync(secondDevice)).StatusCode);

            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.KeepName);
            _app.Clock.Advance(TimeSpan.FromSeconds(1));

            Assert.Equal(HttpStatusCode.Unauthorized, (await AuthRequests.GetMeAsync(secondDevice)).StatusCode);
        }

        [Fact]
        public async Task Delete_Request_MakesEveryEndpointRefuseTheSecondSession()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            HttpClient secondDevice = await _app.CreateLoggedInClientAsync(group.Member.Form.Email, group.Member.Form.Password);
            await DeletionSteps.RequestAsync(group.Member.Client, DeletionRequests.KeepName);
            _app.Clock.Advance(TimeSpan.FromSeconds(1));

            HttpResponseMessage members = await GroupRequests.ListMembersAsync(secondDevice, group.GroupId);

            Assert.Equal(HttpStatusCode.Unauthorized, members.StatusCode);
        }

        [Fact]
        public async Task Delete_Request_DoesNotEndTheSessionOfAnotherAccount()
        {
            SignedInUser leaver = await _app.SignUpAsync("Zorica");
            SignedInUser other = await _app.SignUpAsync("Bojan");

            await DeletionSteps.RequestAsync(leaver.Client, DeletionRequests.KeepName);

            Assert.Equal(HttpStatusCode.OK, (await AuthRequests.GetMeAsync(other.Client)).StatusCode);
        }

        [Fact]
        public async Task Delete_SameEmailRegisteredWhileTheDeletionIsPending_AnswersEmailTaken()
        {
            SignedInUser user = await _app.SignUpAsync();
            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.KeepName);

            HttpResponseMessage response = await AuthRequests.RegisterAsync(_app.CreateClient(), user.Form);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.AUTH_EMAIL_TAKEN);
        }

        [Fact]
        public async Task Delete_Guest_EndsTheGuestSession()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");
            await DeletionSteps.RequestAsync(guest.Client, DeletionRequests.KeepName);

            HttpResponseMessage me = await AuthRequests.GetMeAsync(guest.Client);

            Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        }

        [Fact]
        public async Task Delete_Guest_NewGuestJoinCreatesADifferentAccount()
        {
            (ExpenseGroup group, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");
            await DeletionSteps.RequestAsync(guest.Client, DeletionRequests.KeepName);

            GuestMember again = await GuestSteps.JoinWithNewNameAsync(_app, group.GroupId, "Cvetan");

            Assert.NotEqual(guest.UserId, again.UserId);
            Assert.Equal(StoredDeletion.None, await DeletionRows.ReadAsync(_app, again.UserId));
            Assert.NotNull((await DeletionRows.ReadAsync(_app, guest.UserId)).RequestedAt);
        }
    }
}
