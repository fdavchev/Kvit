using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public static class BlankTokenAnswer
    {
        public static async Task AssertRefusedAsync(HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.INVITE_NOT_FOUND);
                return;
            }

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
