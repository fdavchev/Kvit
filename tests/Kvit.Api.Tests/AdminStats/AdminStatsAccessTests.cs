using System.Net;
using Kvit.Api.Tests.Auth;
using Xunit;

namespace Kvit.Api.Tests.AdminStats
{
    public class AdminStatsAccessTests(AdminStatsApp app) : AdminStatsTestBase(app)
    {
        [Fact]
        public async Task Stats_Anonymous_Answers401()
        {
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AdminStatsRequests.GetAsync(client);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Stats_SignedInWithoutTheAdminRole_Answers403()
        {
            HttpClient client = await _app.CreateRegisteredClientAsync(RegistrationForm.Valid());

            HttpResponseMessage response = await AdminStatsRequests.GetAsync(client);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Stats_SignedInWithoutTheAdminRoleAndWithBadParameters_Answers403NotTheValidationError()
        {
            HttpClient client = await _app.CreateRegisteredClientAsync(RegistrationForm.Valid());

            HttpResponseMessage response = await AdminStatsRequests.GetAsync(client, from: "2026-11-03", to: "2026-11-01", by: "hour");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Stats_Admin_Answers200()
        {
            HttpClient client = await AdminClientAsync();

            HttpResponseMessage response = await AdminStatsRequests.GetAsync(client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Stats_CalledByTheAdmin_WritesNoUsageEvent()
        {
            HttpClient client = await AdminClientAsync();
            int before = await _app.CountUsageEventsAsync();

            await AdminStatsRequests.GetAsync(client, from: "2026-11-01", to: "2026-11-04");
            await AdminStatsRequests.GetAsync(client, from: "2026-11-01", to: "2026-11-04");

            Assert.Equal(before, await _app.CountUsageEventsAsync());
        }
    }
}
