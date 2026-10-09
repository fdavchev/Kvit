namespace Kvit.Domain.Interfaces
{
    public sealed record FetchedExchangeRate(DateOnly RateDate, decimal MkdPerEur);
}
