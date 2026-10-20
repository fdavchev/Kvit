using Kvit.Domain.Entities;

namespace Kvit.Domain.ExchangeRates
{
    public sealed class StoredExchangeRates
    {
        private readonly List<ExchangeRate> _ratesOldestFirst;

        public StoredExchangeRates(IEnumerable<ExchangeRate> rates)
        {
            _ratesOldestFirst = [.. rates.OrderBy(rate => rate.RateDate)];
            if (_ratesOldestFirst.Count == 0)
            {
                throw new InvalidOperationException("No stored exchange rate was found, but the CategoriesAndExchangeRates migration seeds one. Check that every migration was applied to this database.");
            }
        }

        public ExchangeRateSnapshot On(DateOnly date)
        {
            ExchangeRate rate = _ratesOldestFirst.LastOrDefault(candidate => candidate.RateDate <= date) ?? _ratesOldestFirst[0];

            return new ExchangeRateSnapshot(rate.RateDate, rate.MkdPerEur, rate.FetchedAt);
        }
    }
}
