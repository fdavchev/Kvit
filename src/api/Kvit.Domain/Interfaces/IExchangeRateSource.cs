using Kvit.Domain.Results;

namespace Kvit.Domain.Interfaces
{
    public interface IExchangeRateSource
    {
        Task<Result<FetchedExchangeRate>> FetchEurRateAsync(DateOnly date, CancellationToken cancellationToken);
    }
}
