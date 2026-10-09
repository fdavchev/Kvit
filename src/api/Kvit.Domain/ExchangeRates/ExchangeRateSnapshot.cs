namespace Kvit.Domain.ExchangeRates
{
    public sealed record ExchangeRateSnapshot(DateOnly RateDate, decimal MkdPerEur, DateTimeOffset FetchedAt);
}
