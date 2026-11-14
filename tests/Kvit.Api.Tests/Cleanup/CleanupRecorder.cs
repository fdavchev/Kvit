using System.Collections.Concurrent;

namespace Kvit.Api.Tests.Cleanup
{
    public sealed class CleanupRecorder
    {
        private readonly ConcurrentQueue<DateTimeOffset> _runs = new();
        private TaskCompletionSource _hold = CompletedHold();
        private Exception? _nextFailure;
        private int _scopedInstancesMade;
        private int _scopedInstancesDisposed;

        public IReadOnlyList<DateTimeOffset> Runs
        {
            get { return [.. _runs]; }
        }

        public int ScopedInstancesMade
        {
            get { return Volatile.Read(ref _scopedInstancesMade); }
        }

        public int ScopedInstancesDisposed
        {
            get { return Volatile.Read(ref _scopedInstancesDisposed); }
        }

        public void HoldRuns()
        {
            _hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public void ReleaseRuns()
        {
            _hold.TrySetResult();
        }

        public void FailNextRunWith(Exception failure)
        {
            _nextFailure = failure;
        }

        public void NoteScopedInstanceMade()
        {
            Interlocked.Increment(ref _scopedInstancesMade);
        }

        public void NoteScopedInstanceDisposed()
        {
            Interlocked.Increment(ref _scopedInstancesDisposed);
        }

        public async Task RecordRunAsync(DateTimeOffset now)
        {
            _runs.Enqueue(now);
            await _hold.Task;
            Exception? failure = Interlocked.Exchange(ref _nextFailure, null);
            if (failure is not null)
            {
                throw failure;
            }
        }

        private static TaskCompletionSource CompletedHold()
        {
            TaskCompletionSource hold = new();
            hold.SetResult();
            return hold;
        }
    }
}
