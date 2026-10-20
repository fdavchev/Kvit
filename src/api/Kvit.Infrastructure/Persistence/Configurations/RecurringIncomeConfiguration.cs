using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;
using Kvit.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kvit.Infrastructure.Persistence.Configurations
{
    public class RecurringIncomeConfiguration : IEntityTypeConfiguration<RecurringIncome>
    {
        public const string ClientRequestIdIndexName = "ix_recurring_incomes_client_request_id";

        public void Configure(EntityTypeBuilder<RecurringIncome> builder)
        {
            builder.ToTable("recurring_incomes", table =>
            {
                table.HasCheckConstraint("ck_recurring_incomes_amount_minor_positive", "amount_minor > 0");
                table.HasCheckConstraint("ck_recurring_incomes_currency", EnumCheck.OneOf<Currency>("currency"));
                table.HasCheckConstraint("ck_recurring_incomes_frequency", EnumCheck.OneOf<RecurringIncomeFrequency>("frequency"));
                table.HasCheckConstraint("ck_recurring_incomes_day_of_month", $"day_of_month IS NULL OR day_of_month BETWEEN 1 AND {RecurringIncome.LastDayOfMonth}");
                table.HasCheckConstraint("ck_recurring_incomes_day_of_week", $"day_of_week IS NULL OR day_of_week BETWEEN 0 AND {RecurringIncome.LastDayOfWeek}");
                table.HasCheckConstraint("ck_recurring_incomes_ends_on_not_before_starts_on", "ends_on IS NULL OR ends_on >= starts_on");
                table.HasCheckConstraint(
                    "ck_recurring_incomes_day_matches_frequency",
                    $"(frequency = '{nameof(RecurringIncomeFrequency.Monthly)}' AND day_of_month IS NOT NULL AND day_of_week IS NULL) OR (frequency = '{nameof(RecurringIncomeFrequency.Weekly)}' AND day_of_week IS NOT NULL AND day_of_month IS NULL)");
                table.HasCheckConstraint("ck_recurring_incomes_title_length", $"title IS NULL OR char_length(title) BETWEEN 1 AND {RecurringIncomeFields.TitleMaxLength}");
            });

            builder.Property(recurringIncome => recurringIncome.Currency).HasConversion<string>();
            builder.Property(recurringIncome => recurringIncome.Frequency).HasConversion<string>();

            builder.HasIndex(recurringIncome => recurringIncome.ClientRequestId).HasDatabaseName(ClientRequestIdIndexName).IsUnique();

            builder.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(recurringIncome => recurringIncome.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
