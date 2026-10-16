using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Proxy;
using Kvit.Api.Tests.RateLimiting;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class GuestJoinRateLimitTests(ProxiedApp _app) : IClassFixture<ProxiedApp>
    {
        private const int GuestsAllowed = 10;

        [Fact]
        public async Task GuestJoins_UpToTheAllowedNumberFromOneAddress_AllSucceed()
        {
            InviteVisitor visitor = await VisitorWithAGroupAsync();

            for (int guest = 1; guest <= GuestsAllowed; guest++)
            {
                HttpResponseMessage response = await JoinWithNewNameAsync(visitor, guest);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task GuestJoin_AfterTheAllowedNumberFromOneAddress_Answers429RateLimitedWithRetryAfterAndCreatesNoGuest()
        {
            InviteVisitor visitor = await VisitorWithAGroupAsync();
            await JoinManyAsync(visitor, GuestsAllowed);
            int usersBefore = await _app.CountUsersAsync();

            HttpResponseMessage refused = await JoinWithNewNameAsync(visitor, GuestsAllowed + 1);

            await ProblemResponse.AssertAsync(refused, HttpStatusCode.TooManyRequests, ResultCodes.RATE_LIMITED);
            RateLimitRequests.AssertRetryAfterInWholeSeconds(refused);
            Assert.Equal(usersBefore, await _app.CountUsersAsync());
        }

        [Fact]
        public async Task GuestJoinsFailingTheCheapChecks_AreNotCounted_SoTheAllowedNumberOfGuestsStillSucceeds()
        {
            InviteVisitor visitor = await VisitorWithAGroupAsync();
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                HttpResponseMessage rejected = await GroupRequests.JoinAsGuestAsync(visitor.Client, visitor.Token, claimMemberId: Guid.NewGuid(), name: $"Both {attempt}");

                await ProblemResponse.AssertAsync(rejected, HttpStatusCode.BadRequest, ResultCodes.INVITE_GUEST_CHOICE_INVALID);
            }

            for (int guest = 1; guest <= GuestsAllowed; guest++)
            {
                HttpResponseMessage response = await JoinWithNewNameAsync(visitor, guest);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task AnotherAddress_CanStillJoinAsAGuestWhileOneAddressIsRefused()
        {
            InviteVisitor refusedVisitor = await VisitorWithAGroupAsync();
            await JoinManyAsync(refusedVisitor, GuestsAllowed);
            Assert.Equal(HttpStatusCode.TooManyRequests, (await JoinWithNewNameAsync(refusedVisitor, GuestsAllowed + 1)).StatusCode);
            HttpClient otherVisitor = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());

            HttpResponseMessage response = await GroupRequests.JoinAsGuestAsync(otherVisitor, refusedVisitor.Token, name: "Other visitor");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GuestJoins_CountTowardsTheInviteLimitToo()
        {
            InviteVisitor visitor = await VisitorWithAGroupAsync();
            await JoinManyAsync(visitor, GuestsAllowed);
            for (int preview = 1; preview <= 10; preview++)
            {
                Assert.Equal(HttpStatusCode.OK, (await GroupRequests.PreviewAsync(visitor.Client, visitor.Token)).StatusCode);
            }

            HttpResponseMessage refused = await GroupRequests.PreviewAsync(visitor.Client, visitor.Token);

            await ProblemResponse.AssertAsync(refused, HttpStatusCode.TooManyRequests, ResultCodes.RATE_LIMITED);
        }

        private async Task<InviteVisitor> VisitorWithAGroupAsync()
        {
            string address = _app.NewVisitorAddress();
            HttpClient client = _app.CreateClientSending(ProxiedApp.Secret, address);
            Guid ownerId = await UserRows.AddAsync(_app);
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, ownerId);
            string token = (await GroupRows.GroupValueAsync(_app, groupId, "invite_token"))!;

            return new InviteVisitor(client, token, address);
        }

        private Task<HttpResponseMessage> JoinWithNewNameAsync(InviteVisitor visitor, int guest)
        {
            HttpClient guestPhone = _app.CreateClientSending(ProxiedApp.Secret, visitor.Address);

            return GroupRequests.JoinAsGuestAsync(guestPhone, visitor.Token, name: $"Guest {guest}");
        }

        private async Task JoinManyAsync(InviteVisitor visitor, int count)
        {
            for (int guest = 1; guest <= count; guest++)
            {
                await JoinWithNewNameAsync(visitor, guest);
            }
        }
    }
}
