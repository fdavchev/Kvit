using Kvit.Domain.ExchangeRates;
using Kvit.Domain.Results;

namespace Kvit.Domain.Entities
{
    public class ExchangeRate
    {
        public const int ExchangeRateMaxAgeHours = 8;
        public const int MkdPerEurDecimals = 4;
        public const int MkdPerEurPrecision = 10;
        private const decimal MkdPerEurLimit = 1_000_000m;

        private ExchangeRate()
        {
        }

        public DateOnly RateDate { get; private set; }

        public decimal MkdPerEur { get; private set; }

        public DateTimeOffset FetchedAt { get; private set; }

        public static Result<ExchangeRate> Create(DateOnly rateDate, decimal mkdPerEur, DateTimeOffset fetchedAt)
        {
            Result rate = CheckMkdPerEur(mkdPerEur);
            if (!rate.IsSuccess)
            {
                return rate.ToFailure<ExchangeRate>();
            }

            return Result.Ok(new ExchangeRate
            {
                RateDate = rateDate,
                MkdPerEur = mkdPerEur,
                FetchedAt = fetchedAt.ToUniversalTime(),
            });
        }

        public static Result CheckMkdPerEur(decimal mkdPerEur)
        {
            if (mkdPerEur <= 0 || mkdPerEur >= MkdPerEurLimit)
            {
                return Result.Failure($"{mkdPerEur} denars per euro is not a possible exchange rate: it must be more than 0 and less than {MkdPerEurLimit}.", ExchangeRateFailureCodes.EXCHANGE_RATE_INVALID);
            }

            if (decimal.Round(mkdPerEur, MkdPerEurDecimals) != mkdPerEur)
            {
                return Result.Failure($"The exchange rate {mkdPerEur} has more than {MkdPerEurDecimals} decimals.", ExchangeRateFailureCodes.EXCHANGE_RATE_INVALID);
            }

            return Result.Ok();
        }

        public bool NeedsRefresh(DateTimeOffset now)
        {
            return now - FetchedAt >= TimeSpan.FromHours(ExchangeRateMaxAgeHours);
        }
    }
}
