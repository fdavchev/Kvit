namespace Kvit.Contracts.Cleanup
{
    public interface IDataCleanup
    {
        Task<CleanupResponse> RunAsync(DateTimeOffset now, CancellationToken cancellationToken);
    }
}
