using Kvit.Api.Tests.Postgres;
using Kvit.Infrastructure.Cleanup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Kvit.Api.Tests.Cleanup
{
    public class LazyCleanupApp : CleanupApp
    {
        private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(25);

        public LazyCleanupApp(PostgresFixture postgres)
            : base(postgres)
        {
            Action<IServiceCollection>? cleanupAppServices = _factory.AdditionalTestServices;
            _factory.AdditionalTestServices = services =>
            {
                cleanupAppServices?.Invoke(services);
                services.AddSingleton<ILoggerProvider>(Logs);
            };
        }

        public RecordingLoggerProvider Logs { get; } = new();

        public int CountLazyRunsThatEnded()
        {
            return Logs.EntriesOf<LazyDailyCleanup>().Count(entry => entry.Level is LogLevel.Information or LogLevel.Error);
        }

        public Task WaitUntilLazyRunsEndedAsync(int expectedRuns)
        {
            return WaitUntilAsync(() => Task.FromResult(CountLazyRunsThatEnded() >= expectedRuns), $"{expectedRuns} lazy cleanup runs to end");
        }

        public async Task WaitUntilAsync(Func<Task<bool>> condition, string whatIsAwaited)
        {
            DateTime giveUpAt = DateTime.UtcNow + WaitLimit;
            while (DateTime.UtcNow < giveUpAt)
            {
                if (await condition())
                {
                    return;
                }

                await Task.Delay(PollInterval, TestContext.Current.CancellationToken);
            }

            throw new TimeoutException($"Waited {WaitLimit.TotalSeconds} seconds for {whatIsAwaited}, but it did not happen.");
        }
    }
}
