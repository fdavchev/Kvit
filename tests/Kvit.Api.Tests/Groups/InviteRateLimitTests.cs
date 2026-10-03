using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Proxy;
using Kvit.Api.Tests.RateLimiting;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class InviteRateLimitTests(ProxiedApp _app) : IClassFixture<ProxiedApp>
    {
        private const int Allowed = 20;

        [Fact]
        public async Task PreviewAndJoinMixed_UpToTheAllowedNumberFromOneAddress_AreAnsweredNormally()
        {
            InviteVisitor visitor = await SignedInVisitorInAGroupAsync();

            for (int attempt = 1; attempt <= Allowed; attempt++)
            {
                HttpResponseMessage response = await SendPreviewOrJoinAsync(visitor, attempt);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Request_AfterTheAllowedNumberOfMixedPreviewAndJoinFromOneAddress_Answers429RateLimited(bool isJoin)
        {
            InviteVisitor visitor = await SignedInVisitorInAGroupAsync();
            await SendMixAsync(visitor, Allowed);

            HttpResponseMessage refused = await SendPreviewOrJoinAsync(visitor, isJoin ? 2 : 1);

            await ProblemResponse.AssertAsync(refused, HttpStatusCode.TooManyRequests, ResultCodes.RATE_LIMITED);
        }

        [Fact]
        public async Task RefusedRequest_CarriesARetryAfterInWholeSeconds()
        {
            InviteVisitor visitor = await SignedInVisitorInAGroupAsync();
            await SendMixAsync(visitor, Allowed);

            HttpResponseMessage refused = await GroupRequests.PreviewAsync(visitor.Client, visitor.Token);

            RateLimitRequests.AssertRetryAfterInWholeSeconds(refused);
        }

        [Fact]
        public async Task AnotherAddress_IsStillAllowedWhileOneAddressIsRefused()
        {
            InviteVisitor refusedVisitor = await SignedInVisitorInAGroupAsync();
            await SendMixAsync(refusedVisitor, Allowed);
            HttpResponseMessage refused = await GroupRequests.PreviewAsync(refusedVisitor.Client, refusedVisitor.Token);
            Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
            HttpClient otherVisitor = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());

            HttpResponseMessage answer = await GroupRequests.PreviewAsync(otherVisitor, refusedVisitor.Token);

            Assert.Equal(HttpStatusCode.OK, answer.StatusCode);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UnknownTokens_CountTowardsTheLimit(bool isJoin)
        {
            InviteVisitor visitor = await SignedInVisitorInAGroupAsync();
            for (int attempt = 1; attempt <= Allowed; attempt++)
            {
                HttpResponseMessage response = await GroupRequests.PreviewAsync(visitor.Client, $"unknown-token-{attempt}");

                await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.INVITE_NOT_FOUND);
            }

            HttpResponseMessage refused = isJoin
                ? await GroupRequests.JoinAsync(visitor.Client, "unknown-token-last")
                : await GroupRequests.PreviewAsync(visitor.Client, "unknown-token-last");

            await ProblemResponse.AssertAsync(refused, HttpStatusCode.TooManyRequests, ResultCodes.RATE_LIMITED);
        }

        private async Task<InviteVisitor> SignedInVisitorInAGroupAsync()
        {
            HttpClient client = _app.CreateClientSending(ProxiedApp.Secret, _app.NewVisitorAddress());
            HttpResponseMessage response = await AuthRequests.RegisterAsync(client, RegistrationForm.Valid());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Guid userId = (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid();
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, userId);
            string token = (await GroupRows.GroupValueAsync(_app, groupId, "invite_token"))!;

            return new InviteVisitor(client, token);
        }

        private static Task<HttpResponseMessage> SendPreviewOrJoinAsync(InviteVisitor visitor, int attempt)
        {
            return attempt % 2 == 0
                ? GroupRequests.JoinAsync(visitor.Client, visitor.Token)
                : GroupRequests.PreviewAsync(visitor.Client, visitor.Token);
        }

        private static async Task SendMixAsync(InviteVisitor visitor, int count)
        {
            for (int attempt = 1; attempt <= count; attempt++)
            {
                await SendPreviewOrJoinAsync(visitor, attempt);
            }
        }
    }
}
