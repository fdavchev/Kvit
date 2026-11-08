using Xunit;

namespace Kvit.Api.Tests.Cleanup
{
    public abstract class CleanupTestBase(CleanupApp app) : IClassFixture<CleanupApp>, IAsyncLifetime
    {
        protected readonly CleanupApp _app = app;

        public ValueTask InitializeAsync()
        {
            return new ValueTask(_app.ResetAsync());
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
