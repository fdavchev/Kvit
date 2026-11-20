using System.Globalization;
using Kvit.Contracts.Cleanup;
using Kvit.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kvit.Infrastructure.Cleanup
{
    public sealed class LazyDailyCleanup(IServiceScopeFactory _scopeFactory, TimeProvider _timeProvider, ILogger<LazyDailyCleanup> _logger)
    {
        public static readonly TimeSpan WaitAfterFailure = TimeSpan.FromMinutes(15);

        private readonly Lock _lock = new();
        private DateOnly _lastFinishedDay = DateOnly.MinValue;
        private DateTimeOffset _lastFailedAt = DateTimeOffset.MinValue;
        private Task _currentRun = Task.CompletedTask;

        public Task? StartUnlessFinishedOn(DateTimeOffset now)
        {
            DateOnly day = UsageEvent.UtcDayOf(now);
            lock (_lock)
            {
                if (day <= _lastFinishedDay || !_currentRun.IsCompleted || now < _lastFailedAt + WaitAfterFailure)
                {
                    return null;
                }

                _currentRun = Task.Run(() => RunAsync(now, day));
                return _currentRun;
            }
        }

        private async Task RunAsync(DateTimeOffset now, DateOnly day)
        {
            try
            {
                await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
                IDataCleanup dataCleanup = scope.ServiceProvider.GetRequiredService<IDataCleanup>();
                CleanupResponse removed = await dataCleanup.RunAsync(now, CancellationToken.None);
                lock (_lock)
                {
                    _lastFinishedDay = day;
                }

                _logger.LogInformation("The daily cleanup of {Day} (UTC) finished: {Removed}", day.ToString("O", CultureInfo.InvariantCulture), removed);
            }
            catch (Exception exception)
            {
                DateTimeOffset failedAt = _timeProvider.GetUtcNow();
                lock (_lock)
                {
                    _lastFailedAt = failedAt;
                }

                _logger.LogError(exception, "The daily cleanup of {Day} (UTC), started at {Now}, failed at {FailedAt}; the next Active usage event from {RetryFrom} on tries it again.", day.ToString("O", CultureInfo.InvariantCulture), now.ToString("O", CultureInfo.InvariantCulture), failedAt.ToString("O", CultureInfo.InvariantCulture), (failedAt + WaitAfterFailure).ToString("O", CultureInfo.InvariantCulture));
            }
        }
    }
}
