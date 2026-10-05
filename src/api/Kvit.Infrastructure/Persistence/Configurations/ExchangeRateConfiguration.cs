using Kvit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kvit.Infrastructure.Persistence.Configurations
{
    public class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
    {
        public void Configure(EntityTypeBuilder<ExchangeRate> builder)
        {
            builder.ToTable("exchange_rates", table => table.HasCheckConstraint("ck_exchange_rates_mkd_per_eur_positive", "mkd_per_eur > 0"));

            builder.HasKey(exchangeRate => exchangeRate.RateDate);

            builder.Property(exchangeRate => exchangeRate.MkdPerEur).HasPrecision(ExchangeRate.MkdPerEurPrecision, ExchangeRate.MkdPerEurDecimals);

            builder.HasData(new
            {
                RateDate = new DateOnly(2026, 9, 24),
                MkdPerEur = 61.5610m,
                FetchedAt = new DateTimeOffset(2026, 9, 24, 0, 0, 0, TimeSpan.Zero),
            });
        }
    }
}
