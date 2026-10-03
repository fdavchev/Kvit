using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Postgres;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class GroupsApp(PostgresFixture postgres) : AuthApp(postgres)
    {
        public async Task<SignedInUser> SignUpAsync()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = CreateClient();

            HttpResponseMessage response = await AuthRequests.RegisterAsync(client, form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Guid userId = (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid();
            return new SignedInUser(client, userId, form);
        }
    }
}
