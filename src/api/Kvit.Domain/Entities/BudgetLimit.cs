using System.Globalization;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;

namespace Kvit.Domain.Entities
{
    public class BudgetLimit
    {
        public const Currency LimitCurrency = Currency.MKD;

        private BudgetLimit()
        {
        }

        public Guid Id { get; private set; }

        public Guid UserId { get; private set; }

        public Guid? CategoryId { get; private set; }

        public DateOnly EffectiveFrom { get; private set; }

        public long? LimitMinor { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public DateTimeOffset? UpdatedAt { get; private set; }

        public static Result<long> CheckAmount(long? limitMinor)
        {
            long step = CurrencyRules.StepMinorUnits(LimitCurrency);
            if (limitMinor is not long amount || amount <= 0 || amount > ExpenseFields.AmountMaxMinorUnits || amount % step != 0)
            {
                return Result.Failure<long>(
                    $"A limit must be a whole number of {LimitCurrency} minor units, more than 0, at most {ExpenseFields.AmountMaxMinorUnits} and a multiple of {step}; got {limitMinor?.ToString(CultureInfo.InvariantCulture) ?? "no whole number"}.",
                    ResultCodes.BUDGET_LIMIT_AMOUNT_INVALID);
            }

            return Result.Ok(amount);
        }

        public static DateOnly FirstDayOfMonthOf(DateOnly day)
        {
            return new DateOnly(day.Year, day.Month, 1);
        }

        public static BudgetLimit Create(Guid userId, Guid? categoryId, DateOnly effectiveFrom, long? limitMinor, DateTimeOffset createdAt)
        {
            if (effectiveFrom.Day != 1)
            {
                throw new InvalidOperationException($"A limit starts applying on the first day of a month, not on {effectiveFrom.ToString(ExpenseFields.DateFormat, CultureInfo.InvariantCulture)}.");
            }

            return new BudgetLimit
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                CategoryId = categoryId,
                EffectiveFrom = effectiveFrom,
                LimitMinor = limitMinor,
                CreatedAt = createdAt,
            };
        }

        public void Change(long? limitMinor, DateTimeOffset updatedAt)
        {
            if (LimitMinor == limitMinor)
            {
                return;
            }

            LimitMinor = limitMinor;
            UpdatedAt = updatedAt;
        }
    }
}
