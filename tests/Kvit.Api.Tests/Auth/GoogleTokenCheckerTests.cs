using System.Net;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Auth;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class GoogleTokenCheckerTests
    {
        [Theory]
        [InlineData("not-a-jwt")]
        [InlineData("   ")]
        [InlineData("")]
        [InlineData("a.b.c")]
        [InlineData("YWJj.YWJj.YWJj")]
        public async Task CheckAsync_TokenThatIsNotAJwt_FailsWithGoogleTokenInvalidAndNeedsNoNetwork(string idToken)
        {
            GoogleTokenChecker checker = new("any-client-id");

            Result<GoogleIdentity> result = await checker.CheckAsync(idToken, TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultCodes.AUTH_GOOGLE_TOKEN_INVALID, result.ErrorCode);
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        }
    }
}
