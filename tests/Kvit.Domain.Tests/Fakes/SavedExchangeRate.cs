namespace Kvit.Domain.Tests.Fakes
{
    public sealed record SavedExchangeRate(DateOnly RateDate, decimal MkdPerEur, DateTimeOffset FetchedAt);
}
