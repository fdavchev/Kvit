using System.Net;
using System.Text.Json;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public static class ProblemResponse
    {
        public static async Task AssertAsync(HttpResponseMessage response, HttpStatusCode expectedStatus, string expectedErrorCode)
        {
            Assert.Equal(expectedStatus, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal((int)expectedStatus, body.GetProperty("status").GetInt32());
            Assert.Equal(expectedErrorCode, body.GetProperty("errorCode").GetString());
        }
    }
}
