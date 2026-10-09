using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Tests.Services.ExchangeRates;

namespace Kvit.Domain.Tests.Fakes
{
    public sealed class FakeExchangeRateRepository : IExchangeRateRepository
    {
        private readonly SortedDictionary<DateOnly, ExchangeRate> _rowsByDate = [];

        public List<SavedExchangeRate> Saved { get; } = [];

        public IReadOnlyCollection<ExchangeRate> Rows => _rowsByDate.Values;

        public void Seed(DateOnly rateDate, decimal mkdPerEur, DateTimeOffset fetchedAt)
        {
            _rowsByDate[rateDate] = ExchangeRateTestData.Row(rateDate, mkdPerEur, fetchedAt);
        }

        public Task<ExchangeRate> GetLatestAsync(CancellationToken cancellationToken)
        {
            if (_rowsByDate.Count == 0)
            {
                throw new InvalidOperationException("The exchange_rates table is empty, but a migration seeds it.");
            }

            return Task.FromResult(_rowsByDate.Last().Value);
        }

        public Task SaveFetchedAsync(DateOnly rateDate, decimal mkdPerEur, DateTimeOffset fetchedAt, CancellationToken cancellationToken)
        {
            Saved.Add(new SavedExchangeRate(rateDate, mkdPerEur, fetchedAt));
            _rowsByDate[rateDate] = ExchangeRateTestData.Row(rateDate, mkdPerEur, fetchedAt);

            return Task.CompletedTask;
        }
    }
}
