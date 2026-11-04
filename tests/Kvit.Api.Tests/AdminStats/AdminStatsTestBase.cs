using System.Text.Json;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.AdminStats
{
    public abstract class AdminStatsTestBase(AdminStatsApp app) : IClassFixture<AdminStatsApp>, IAsyncLifetime
    {
        protected readonly AdminStatsApp _app = app;
        private SignedInUser? _admin;

        public ValueTask InitializeAsync()
        {
            return new ValueTask(_app.ResetAsync());
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        protected async Task<HttpClient> AdminClientAsync()
        {
            _admin ??= await _app.SignInAdminAsync();

            return _admin.Client;
        }

        protected async Task<JsonElement> StatsAsync(string? from = null, string? to = null, string? by = null)
        {
            return await AdminStatsRequests.ReadAsync(await AdminClientAsync(), from, to, by);
        }
    }
}
