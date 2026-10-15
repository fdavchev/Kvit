using Kvit.Application.Dispatching;
using Kvit.Application.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Services.UsageEvents;
using Kvit.Domain.UsageEvents;

namespace Kvit.Application.Commands.Me
{
    public sealed class RecordActiveCommandHandler(
        ICurrentUserProvider _currentUserProvider,
        RecordActive _recordActive,
        ActiveDayMemory _activeDayMemory,
        IUnitOfWork _unitOfWork,
        TimeProvider _timeProvider) : ICommandHandler<RecordActiveCommand>
    {
        public async Task<Result> Handle(RecordActiveCommand command, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return userId;
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            DateOnly day = UsageEvent.UtcDayOf(now);
            if (_activeDayMemory.IsCounted(userId.Value, day))
            {
                return Result.Ok();
            }

            Result recorded;
            try
            {
                recorded = await _unitOfWork.RunInTransactionAsync(() => _recordActive.Execute(userId.Value, now, cancellationToken), cancellationToken);
            }
            catch (ActiveDayAlreadySavedException)
            {
                recorded = Result.Ok();
            }

            if (recorded.IsSuccess)
            {
                _activeDayMemory.Remember(userId.Value, day);
            }

            return recorded;
        }
    }
}
