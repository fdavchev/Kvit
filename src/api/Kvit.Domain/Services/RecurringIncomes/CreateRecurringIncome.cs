using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.RecurringIncomes
{
    public sealed class CreateRecurringIncome(
        IRecurringIncomeRepository _incomes,
        TimeProvider _timeProvider)
    {
        public async Task<Result<Guid>> Execute(Guid userId, Guid clientRequestId, RecurringIncomeInput input, CancellationToken cancellationToken)
        {
            RecurringIncome? earlier = await _incomes.FindByClientRequestIdAsync(clientRequestId, cancellationToken);
            if (earlier is not null)
            {
                return earlier.AnswerToRepeatBy(userId);
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            Result<RecurringIncome> created = RecurringIncome.Create(userId, input, clientRequestId, DateOnly.FromDateTime(now.UtcDateTime), now);
            if (!created.IsSuccess)
            {
                return created.ToFailure<Guid>();
            }

            int activeCount = await _incomes.LockAndCountActiveOfAsync(userId, cancellationToken);
            Result room = RecurringIncome.CheckRoomForOneMore(activeCount);
            if (!room.IsSuccess)
            {
                return room.ToFailure<Guid>();
            }

            _incomes.Add(created.Value);

            return Result.Ok(created.Value.Id);
        }
    }
}
