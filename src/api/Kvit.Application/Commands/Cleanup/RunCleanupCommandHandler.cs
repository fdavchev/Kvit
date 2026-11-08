using Kvit.Application.Dispatching;
using Kvit.Contracts.Cleanup;
using Kvit.Domain.Results;

namespace Kvit.Application.Commands.Cleanup
{
    public sealed class RunCleanupCommandHandler(
        IDataCleanup _dataCleanup,
        TimeProvider _timeProvider) : ICommandHandler<RunCleanupCommand, CleanupResponse>
    {
        public async Task<Result<CleanupResponse>> Handle(RunCleanupCommand command, CancellationToken cancellationToken)
        {
            CleanupResponse response = await _dataCleanup.RunAsync(_timeProvider.GetUtcNow(), cancellationToken);
            return Result.Ok(response);
        }
    }
}
