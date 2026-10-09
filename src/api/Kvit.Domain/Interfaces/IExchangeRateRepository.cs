using Kvit.Domain.Entities;

namespace Kvit.Domain.Interfaces
{
    public interface IExchangeRateRepository
    {
        Task<ExchangeRate> GetLatestAsync(CancellationToken cancellationToken);

        Task SaveFetchedAsync(DateOnly rateDate, decimal mkdPerEur, DateTimeOffset fetchedAt, CancellationToken cancellationToken);
    }
}
