using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Infrastructure.Repositories
{
    public sealed class ExchangeRateRepository(AppDbContext _context) : IExchangeRateRepository
    {
        public async Task<ExchangeRate> GetLatestAsync(CancellationToken cancellationToken)
        {
            ExchangeRate? latest = await _context.ExchangeRates
                .AsNoTracking()
                .OrderByDescending(exchangeRate => exchangeRate.RateDate)
                .FirstOrDefaultAsync(cancellationToken);

            return latest ?? throw new InvalidOperationException(
                "The exchange_rates table is empty, but the CategoriesAndExchangeRates migration seeds it. Check that every migration was applied to this database.");
        }

        public async Task SaveFetchedAsync(DateOnly rateDate, decimal mkdPerEur, DateTimeOffset fetchedAt, CancellationToken cancellationToken)
        {
            Result<ExchangeRate> created = ExchangeRate.Create(rateDate, mkdPerEur, fetchedAt);
            if (!created.IsSuccess)
            {
                throw new InvalidOperationException($"The fetched exchange rate for {rateDate} cannot be saved: {created.Error}");
            }

            ExchangeRate row = created.Value;
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO exchange_rates (rate_date, mkd_per_eur, fetched_at)
                VALUES ({row.RateDate}, {row.MkdPerEur}, {row.FetchedAt})
                ON CONFLICT (rate_date) DO UPDATE SET mkd_per_eur = EXCLUDED.mkd_per_eur, fetched_at = EXCLUDED.fetched_at
                """,
                cancellationToken);
        }
    }
}
