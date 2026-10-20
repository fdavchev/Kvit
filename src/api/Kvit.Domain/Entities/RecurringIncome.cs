using Kvit.Domain.MoneyRules;
using Kvit.Domain.PersonalEntries;
using Kvit.Domain.Results;

namespace Kvit.Domain.Entities
{
    public class RecurringIncome
    {
        public const int LastDayOfMonth = 31;
        public const int LastDayOfWeek = 6;
        public const int ActiveLimit = 20;

        private RecurringIncome()
        {
        }

        public Guid Id { get; private set; }

        public Guid UserId { get; private set; }

        public long AmountMinor { get; private set; }

        public Currency Currency { get; private set; }

        public string? Title { get; private set; }

        public RecurringIncomeFrequency Frequency { get; private set; }

        public int? DayOfMonth { get; private set; }

        public int? DayOfWeek { get; private set; }

        public DateOnly StartsOn { get; private set; }

        public DateOnly? EndsOn { get; private set; }

        public Guid ClientRequestId { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public DateTimeOffset? StoppedAt { get; private set; }

        public static Result<RecurringIncome> Create(Guid userId, RecurringIncomeInput input, Guid clientRequestId, DateOnly utcToday, DateTimeOffset createdAt)
        {
            Result<RecurringIncomeFields> fields = RecurringIncomeFields.Create(input, utcToday);
            if (!fields.IsSuccess)
            {
                return fields.ToFailure<RecurringIncome>();
            }

            RecurringIncome income = new()
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                ClientRequestId = clientRequestId,
                CreatedAt = createdAt,
            };
            income.ApplyFields(fields.Value);

            return Result.Ok(income);
        }

        public static Result<T> NotFound<T>(Guid recurringIncomeId)
        {
            return Result.NotFound<T>($"Repeating income {recurringIncomeId} is not one of your active repeating incomes.", ResultCodes.RECURRING_INCOME_NOT_FOUND);
        }

        public static Result CheckRoomForOneMore(int activeCount)
        {
            if (activeCount >= ActiveLimit)
            {
                return Result.Failure($"You already have {activeCount} active repeating incomes; the most is {ActiveLimit}. Stop one first.", ResultCodes.RECURRING_INCOME_LIMIT_REACHED);
            }

            return Result.Ok();
        }

        public Result<Guid> AnswerToRepeatBy(Guid userId)
        {
            if (UserId != userId)
            {
                return Result.Failure<Guid>($"The request id {ClientRequestId} was already used by another person.", ResultCodes.RECURRING_INCOME_CLIENT_REQUEST_ID_USED);
            }

            return Result.Ok(Id);
        }

        public Result<RecurringIncome> Update(RecurringIncomeInput input, bool hasEntries, DateOnly utcToday)
        {
            Result<RecurringIncomeFields> fields = RecurringIncomeFields.CreateForEdit(input, utcToday, StartsOn, hasEntries);
            if (!fields.IsSuccess)
            {
                return fields.ToFailure<RecurringIncome>();
            }

            ApplyFields(fields.Value);

            return Result.Ok(this);
        }

        public void Stop(DateTimeOffset stoppedAt)
        {
            StoppedAt ??= stoppedAt;
        }

        public Result Restart(DateTimeOffset now)
        {
            if (StoppedAt is not DateTimeOffset stoppedAt)
            {
                return Result.Failure($"Repeating income {Id} is not stopped.", ResultCodes.RECURRING_INCOME_NOT_STOPPED);
            }

            if (now >= Expense.RestorableUntil(stoppedAt))
            {
                return Result.Failure($"Repeating income {Id} was stopped more than {Expense.DeletedExpenseRestoreDays} days ago and can no longer be restarted.", ResultCodes.RECURRING_INCOME_RESTART_EXPIRED);
            }

            StoppedAt = null;

            return Result.Ok();
        }

        private void ApplyFields(RecurringIncomeFields fields)
        {
            AmountMinor = fields.Amount.MinorUnits;
            Currency = fields.Amount.Currency;
            Title = fields.Title;
            Frequency = fields.Frequency;
            DayOfMonth = fields.DayOfMonth;
            DayOfWeek = fields.DayOfWeek;
            StartsOn = fields.StartsOn;
            EndsOn = fields.EndsOn;
        }
    }
}
