using System.Globalization;
using Kvit.Domain.Entities;
using Kvit.Domain.ExchangeRates;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;
using Microsoft.Extensions.Logging;

namespace Kvit.Domain.Services.ExchangeRates
{
    public sealed class GetCurrentExchangeRate(
        IExchangeRateRepository _exchangeRates,
        IExchangeRateSource _source,
        ExchangeRateRefreshGate _refreshGate,
        TimeProvider _timeProvider,
        ILogger<GetCurrentExchangeRate> _logger)
    {
        public const int LookBackDays = 7;

        public async Task<Result<ExchangeRateSnapshot>> Execute(CancellationToken cancellationToken)
        {
            ExchangeRate newest = await _exchangeRates.GetLatestAsync(cancellationToken);
            DateTimeOffset now = _timeProvider.GetUtcNow();
            if (!newest.NeedsRefresh(now) || !_refreshGate.CanTry())
            {
                return Result.Ok(SnapshotOf(newest));
            }

            DateOnly utcToday = DateOnly.FromDateTime(now.UtcDateTime);
            Result<FetchedExchangeRate> fetched = await FetchLatestPublishedAsync(utcToday, cancellationToken);
            if (!fetched.IsSuccess)
            {
                _logger.LogError(
                    "Could not refresh the euro exchange rate for {UtcToday} or the {LookBackDays} days before it, so the rate of {RateDate} fetched at {FetchedAt} is kept. Cause: {Cause}",
                    utcToday.ToString("O", CultureInfo.InvariantCulture),
                    LookBackDays,
                    newest.RateDate.ToString("O", CultureInfo.InvariantCulture),
                    newest.FetchedAt.ToString("O", CultureInfo.InvariantCulture),
                    fetched.Error);
                _refreshGate.RecordFailure();
                return Result.Ok(SnapshotOf(newest));
            }

            FetchedExchangeRate rate = fetched.Value;
            await _exchangeRates.SaveFetchedAsync(rate.RateDate, rate.MkdPerEur, now, cancellationToken);

            return Result.Ok(new ExchangeRateSnapshot(rate.RateDate, rate.MkdPerEur, now));
        }

        private async Task<Result<FetchedExchangeRate>> FetchLatestPublishedAsync(DateOnly utcToday, CancellationToken cancellationToken)
        {
            Result<FetchedExchangeRate> fetched = await _source.FetchEurRateAsync(utcToday, cancellationToken);
            for (int daysBack = 1; daysBack <= LookBackDays && IsNotPublished(fetched); daysBack++)
            {
                fetched = await _source.FetchEurRateAsync(utcToday.AddDays(-daysBack), cancellationToken);
            }

            return fetched;
        }

        private static bool IsNotPublished(Result<FetchedExchangeRate> fetched)
        {
            return !fetched.IsSuccess && fetched.ErrorCode == ExchangeRateFailureCodes.EXCHANGE_RATE_NOT_PUBLISHED;
        }

        private static ExchangeRateSnapshot SnapshotOf(ExchangeRate exchangeRate)
        {
            return new ExchangeRateSnapshot(exchangeRate.RateDate, exchangeRate.MkdPerEur, exchangeRate.FetchedAt);
        }
    }
}
