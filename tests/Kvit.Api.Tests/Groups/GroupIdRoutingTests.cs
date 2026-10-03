using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class GroupIdRoutingTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Theory]
        [InlineData("GET", "/api/groups/not-a-guid")]
        [InlineData("PUT", "/api/groups/not-a-guid")]
        [InlineData("DELETE", "/api/groups/not-a-guid")]
        [InlineData("POST", "/api/groups/not-a-guid/restore")]
        public async Task GroupId_ThatIsNotAGuid_Answers404(string method, string path)
        {
            SignedInUser user = await _app.SignUpAsync();
            using HttpRequestMessage request = new(new HttpMethod(method), path)
            {
                Content = JsonContent.Create(new { name = "Greece trip", emoji = "x", currency = "MKD" }),
            };

            HttpResponseMessage response = await user.Client.SendAsync(request, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
