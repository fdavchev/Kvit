using Kvit.Domain.Results;

namespace Kvit.Domain.MoneyRules
{
    public sealed record Money
    {
        private Money(long minorUnits, Currency currency)
        {
            MinorUnits = minorUnits;
            Currency = currency;
        }

        public long MinorUnits { get; }

        public Currency Currency { get; }

        public static Result<Money> Create(long minorUnits, Currency currency)
        {
            long step = CurrencyRules.StepMinorUnits(currency);
            if (minorUnits % step != 0)
            {
                return Result.Failure<Money>(
                    $"{CurrencyRules.FormatAmount(minorUnits, currency)} {currency} is not a whole step of {CurrencyRules.FormatAmount(step, currency)} {currency} ({minorUnits} minor units).",
                    ResultCodes.MONEY_NOT_ON_CURRENCY_STEP);
            }

            return Result.Ok(new Money(minorUnits, currency));
        }

        public static Money Zero(Currency currency) => Create(0, currency).Value;

        public Result<Money> Add(Money other)
        {
            if (other.Currency != Currency)
            {
                return CurrencyMismatch($"add {Describe(other)} to {Describe(this)}");
            }

            return Result.Ok(new Money(checked(MinorUnits + other.MinorUnits), Currency));
        }

        public Result<Money> Subtract(Money other)
        {
            if (other.Currency != Currency)
            {
                return CurrencyMismatch($"subtract {Describe(other)} from {Describe(this)}");
            }

            return Result.Ok(new Money(checked(MinorUnits - other.MinorUnits), Currency));
        }

        private static string Describe(Money money) =>
            $"{CurrencyRules.FormatAmount(money.MinorUnits, money.Currency)} {money.Currency}";

        private static Result<Money> CurrencyMismatch(string operation) =>
            Result.Failure<Money>($"Cannot {operation}: MKD and EUR are never mixed.", ResultCodes.MONEY_CURRENCY_MISMATCH);
    }
}
