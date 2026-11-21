using Kvit.Contracts.Cleanup;

namespace Kvit.Api.Tests.Cleanup
{
    public sealed class ScopedFakeDataCleanup : IDataCleanup, IDisposable
    {
        private readonly CleanupRecorder _recorder;

        public ScopedFakeDataCleanup(CleanupRecorder recorder)
        {
            _recorder = recorder;
            _recorder.NoteScopedInstanceMade();
        }

        public async Task<CleanupResponse> RunAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            await _recorder.RecordRunAsync(now);

            return new CleanupResponse(0, 0, 0, 0, 0, 0, 0);
        }

        public void Dispose()
        {
            _recorder.NoteScopedInstanceDisposed();
        }
    }
}
