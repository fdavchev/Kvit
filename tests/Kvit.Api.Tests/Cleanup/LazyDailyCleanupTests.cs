using Kvit.Api.Tests.Expenses;
using Kvit.Contracts.Cleanup;
using Kvit.Infrastructure.Cleanup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Kvit.Api.Tests.Cleanup
{
    public sealed class LazyDailyCleanupTests : IDisposable
    {
        private const int StartsAtTheSameMoment = 32;
        private static readonly DateTimeOffset Noon = new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset StartOfTheDay = new(2026, 11, 14, 0, 0, 0, TimeSpan.Zero);
        private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

        private readonly CleanupRecorder _recorder = new();
        private readonly AdjustableClock _clock = new(Noon);
        private readonly RecordingLoggerProvider _logs = new();
        private readonly ServiceProvider _services;
        private readonly LoggerFactory _loggerFactory;
        private readonly LazyDailyCleanup _cleanup;

        public LazyDailyCleanupTests()
        {
            ServiceCollection services = new();
            services.AddScoped<IDataCleanup>(_ => new ScopedFakeDataCleanup(_recorder));
            _services = services.BuildServiceProvider();
            _loggerFactory = new LoggerFactory([_logs]);
            _cleanup = new LazyDailyCleanup(_services.GetRequiredService<IServiceScopeFactory>(), _clock, _loggerFactory.CreateLogger<LazyDailyCleanup>());
        }

        public void Dispose()
        {
            _recorder.ReleaseRuns();
            _loggerFactory.Dispose();
            _services.Dispose();
        }

        [Fact]
        public async Task Start_FirstTimeOnAUtcDay_RunsTheCleanupOnceWithThatMoment()
        {
            Task? run = _cleanup.StartUnlessFinishedOn(Noon);

            await WaitForAsync(run);
            Assert.Equal([Noon], _recorder.Runs);
        }

        [Fact]
        public async Task Start_AgainOnTheSameUtcDayAfterTheRunFinished_DoesNotRunItAgain()
        {
            await WaitForAsync(_cleanup.StartUnlessFinishedOn(StartOfTheDay));

            Task? second = _cleanup.StartUnlessFinishedOn(StartOfTheDay.AddDays(1) - OneSecond);

            Assert.Null(second);
            Assert.Equal([StartOfTheDay], _recorder.Runs);
        }

        [Fact]
        public async Task Start_OneSecondAfterUtcMidnight_RunsItAgainForTheNewDay()
        {
            DateTimeOffset lastSecond = StartOfTheDay.AddDays(1) - OneSecond;
            await WaitForAsync(_cleanup.StartUnlessFinishedOn(lastSecond));

            Task? next = _cleanup.StartUnlessFinishedOn(lastSecond + OneSecond);

            await WaitForAsync(next);
            Assert.Equal([lastSecond, lastSecond + OneSecond], _recorder.Runs);
        }

        [Fact]
        public async Task Start_ManyTimesOnThreeDaysInARow_RunsOncePerDay()
        {
            for (int day = 0; day < 3; day++)
            {
                await WaitForAsync(_cleanup.StartUnlessFinishedOn(Noon.AddDays(day)));
                Assert.Null(_cleanup.StartUnlessFinishedOn(Noon.AddDays(day).AddHours(1)));
            }

            Assert.Equal([Noon, Noon.AddDays(1), Noon.AddDays(2)], _recorder.Runs);
        }

        [Fact]
        public async Task Start_ForAnEarlierDayThanTheLastFinishedRun_DoesNotRunIt()
        {
            await WaitForAsync(_cleanup.StartUnlessFinishedOn(Noon));

            Task? earlier = _cleanup.StartUnlessFinishedOn(Noon.AddDays(-1));

            Assert.Null(earlier);
            Assert.Single(_recorder.Runs);
        }

        [Fact]
        public async Task Start_ManyTimesAtTheSameMoment_StartsExactlyOneRun()
        {
            _recorder.HoldRuns();

            Task?[] starts = await Task.WhenAll(Enumerable.Range(0, StartsAtTheSameMoment)
                .Select(_ => Task.Run<Task?>(() => _cleanup.StartUnlessFinishedOn(Noon))));
            _recorder.ReleaseRuns();

            Task run = Assert.Single(starts.OfType<Task>());
            await WaitForAsync(run);
            Assert.Single(_recorder.Runs);
        }

        [Fact]
        public async Task Start_ReturnsWhileTheCleanupIsStillRunning()
        {
            _recorder.HoldRuns();

            Task? run = _cleanup.StartUnlessFinishedOn(Noon);

            Assert.NotNull(run);
            Assert.False(run.IsCompleted);
            _recorder.ReleaseRuns();
            await WaitForAsync(run);
        }

        [Fact]
        public async Task Start_OnTheNextDayWhileTheRunOfTheDayBeforeIsStillGoing_DoesNotStartASecondRunAtTheSameTime()
        {
            _recorder.HoldRuns();
            Task? first = _cleanup.StartUnlessFinishedOn(Noon);

            Task? overlapping = _cleanup.StartUnlessFinishedOn(Noon.AddDays(1));

            Assert.Null(overlapping);
            _recorder.ReleaseRuns();
            await WaitForAsync(first);
            Assert.Equal([Noon], _recorder.Runs);
        }

        [Fact]
        public async Task Start_OnTheNextDayAfterTheRunOfTheDayBeforeFinished_RunsIt()
        {
            _recorder.HoldRuns();
            Task? first = _cleanup.StartUnlessFinishedOn(Noon);
            Assert.Null(_cleanup.StartUnlessFinishedOn(Noon.AddDays(1)));
            _recorder.ReleaseRuns();
            await WaitForAsync(first);

            Task? next = _cleanup.StartUnlessFinishedOn(Noon.AddDays(1).AddMinutes(5));

            await WaitForAsync(next);
            Assert.Equal([Noon, Noon.AddDays(1).AddMinutes(5)], _recorder.Runs);
        }

        [Fact]
        public async Task Start_WhenTheRunFails_LogsAnErrorThatCarriesTheException()
        {
            InvalidOperationException failure = new("The database is down.");
            _recorder.FailNextRunWith(failure);

            await WaitForAsync(_cleanup.StartUnlessFinishedOn(Noon));

            RecordedLogEntry error = Assert.Single(_logs.EntriesOf<LazyDailyCleanup>(), entry => entry.Level == LogLevel.Error);
            Assert.Same(failure, error.Exception);
            Assert.Contains("tries it again", error.Message);
        }

        [Fact]
        public async Task Start_WhenTheRunFails_TheReturnedRunStillEndsWithoutAnException()
        {
            _recorder.FailNextRunWith(new InvalidOperationException("The database is down."));

            Task? run = _cleanup.StartUnlessFinishedOn(Noon);

            await WaitForAsync(run);
            Assert.True(run!.IsCompletedSuccessfully);
        }

        [Fact]
        public async Task Start_AgainOnTheSameDayAfterAFailedRun_RunsItAgain()
        {
            _recorder.FailNextRunWith(new InvalidOperationException("The database is down."));
            await WaitForAsync(_cleanup.StartUnlessFinishedOn(Noon));

            Task? retry = _cleanup.StartUnlessFinishedOn(Noon + LazyDailyCleanup.WaitAfterFailure);

            await WaitForAsync(retry);
            Assert.Equal([Noon, Noon + LazyDailyCleanup.WaitAfterFailure], _recorder.Runs);
            Assert.Null(_cleanup.StartUnlessFinishedOn(Noon.AddHours(1)));
        }

        [Fact]
        public async Task Start_LessThanFifteenMinutesAfterAFailedRun_DoesNotRunItAgain()
        {
            _recorder.FailNextRunWith(new InvalidOperationException("The database is down."));
            await WaitForAsync(_cleanup.StartUnlessFinishedOn(Noon));

            Task? tooSoon = _cleanup.StartUnlessFinishedOn(Noon + LazyDailyCleanup.WaitAfterFailure - OneSecond);

            Assert.Null(tooSoon);
            Assert.Equal([Noon], _recorder.Runs);
        }

        [Fact]
        public async Task Start_AfterAFailedRun_WaitsFifteenMinutesFromTheMomentItFailedNotFromItsStart()
        {
            DateTimeOffset failedAt = Noon.AddMinutes(5);
            _clock.SetTo(failedAt);
            _recorder.FailNextRunWith(new InvalidOperationException("The database is down."));
            await WaitForAsync(_cleanup.StartUnlessFinishedOn(Noon));

            Task? tooSoon = _cleanup.StartUnlessFinishedOn(failedAt + LazyDailyCleanup.WaitAfterFailure - OneSecond);
            Task? retry = _cleanup.StartUnlessFinishedOn(failedAt + LazyDailyCleanup.WaitAfterFailure);

            Assert.Null(tooSoon);
            await WaitForAsync(retry);
            Assert.Equal([Noon, failedAt + LazyDailyCleanup.WaitAfterFailure], _recorder.Runs);
        }

        [Fact]
        public async Task Start_WhenTheRunSucceeds_LogsNoErrorAndOneInformationLine()
        {
            await WaitForAsync(_cleanup.StartUnlessFinishedOn(Noon));

            IReadOnlyList<RecordedLogEntry> entries = _logs.EntriesOf<LazyDailyCleanup>();
            Assert.DoesNotContain(entries, entry => entry.Level >= LogLevel.Warning);
            Assert.Single(entries, entry => entry.Level == LogLevel.Information);
        }

        [Fact]
        public async Task Run_GetsItsOwnScopeAndDisposesItWhenItEnds()
        {
            await WaitForAsync(_cleanup.StartUnlessFinishedOn(Noon));
            await WaitForAsync(_cleanup.StartUnlessFinishedOn(Noon.AddDays(1)));

            Assert.Equal(2, _recorder.ScopedInstancesMade);
            Assert.Equal(2, _recorder.ScopedInstancesDisposed);
        }

        [Fact]
        public async Task Run_ThatFails_StillDisposesItsScope()
        {
            _recorder.FailNextRunWith(new InvalidOperationException("The database is down."));

            await WaitForAsync(_cleanup.StartUnlessFinishedOn(Noon));

            Assert.Equal(1, _recorder.ScopedInstancesMade);
            Assert.Equal(1, _recorder.ScopedInstancesDisposed);
        }

        private static async Task WaitForAsync(Task? run)
        {
            Assert.NotNull(run);
            await run.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);
        }
    }
}
