using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.OwnerTools
{
    public class OwnerToolsEmptyLogTests(OwnerToolsApp _app) : IClassFixture<OwnerToolsApp>
    {
        [Fact]
        public async Task Log_BeforeAnyActionWasTaken_AnswersAnEmptyList()
        {
            SignedInUser admin = await _app.SharedAdminAsync();

            HttpResponseMessage response = await OwnerToolsRequests.LogAsync(admin.Client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement log = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(JsonValueKind.Array, log.ValueKind);
            Assert.Equal(0, log.GetArrayLength());
        }
    }
}
