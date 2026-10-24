using Kvit.Domain.Entities;
using Kvit.Domain.ExchangeRates;

namespace Kvit.Domain.Interfaces
{
    public interface IExchangeRateRepository
    {
        Task<ExchangeRate> GetLatestAsync(CancellationToken cancellationToken);

        Task<StoredExchangeRates> GetStoredCoveringAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken);

        Task SaveFetchedAsync(DateOnly rateDate, decimal mkdPerEur, DateTimeOffset fetchedAt, CancellationToken cancellationToken);
    }
}
