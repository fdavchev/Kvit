using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Hosting;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Postgres;

namespace Kvit.Api.Tests.Cleanup
{
    public class CleanupApp : ExpensesApp
    {
        public const string Secret = "test-cleanup-secret-0123456789abcdef";

        private readonly KvitApiFactory _unconfiguredFactory;

        public CleanupApp(PostgresFixture postgres)
            : base(postgres)
        {
            _factory.CleanupSecret = Secret;
            _unconfiguredFactory = _factory.CreateAnotherInstanceOnTheSameDatabase();
            _unconfiguredFactory.AdditionalTestServices = _factory.AdditionalTestServices;
        }

        public DateTimeOffset Now
        {
            get { return Clock.GetUtcNow(); }
        }

        public override async ValueTask DisposeAsync()
        {
            await _unconfiguredFactory.DisposeAsync();
            await base.DisposeAsync();
        }

        public HttpClient CreateClientOfTheHostWithoutTheSecret()
        {
            return _unconfiguredFactory.CreateClient(HttpsWithCookies());
        }

        public async Task ResetAsync()
        {
            Clock.SetTo(ClockStart);
            await ExpenseRows.AllowAllShareChangesAsync(this);
            await CleanupRows.RemoveSoftDeletedRowsAsync(this);
            await CleanupRows.ForgetPendingDeletionRequestsAsync(this);
            await CleanupRows.DetachAllUsageEventsAsync(this);
        }

        public async Task<CleanupResult> RunCleanupAsync()
        {
            HttpResponseMessage response = await CleanupRequests.PostAsync(CreateClientWithoutCookies(), Secret);

            return await CleanupRequests.ReadResultAsync(response);
        }
    }
}
