using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;
using Kvit.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kvit.Infrastructure.Persistence.Configurations
{
    public class BudgetLimitConfiguration : IEntityTypeConfiguration<BudgetLimit>
    {
        public const string UserCategoryMonthIndexName = "ix_budget_limits_user_id_category_id_effective_from";

        public void Configure(EntityTypeBuilder<BudgetLimit> builder)
        {
            builder.ToTable("budget_limits", table =>
            {
                table.HasCheckConstraint("ck_budget_limits_effective_from_first_day", "extract(day from effective_from) = 1");
                table.HasCheckConstraint(
                    "ck_budget_limits_limit_minor_valid",
                    $"limit_minor IS NULL OR (limit_minor > 0 AND limit_minor <= {ExpenseFields.AmountMaxMinorUnits} AND limit_minor % {CurrencyRules.StepMinorUnits(BudgetLimit.LimitCurrency)} = 0)");
            });

            builder.HasIndex(limit => new { limit.UserId, limit.CategoryId, limit.EffectiveFrom })
                .HasDatabaseName(UserCategoryMonthIndexName)
                .IsUnique()
                .AreNullsDistinct(false);

            builder.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(limit => limit.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Category>()
                .WithMany()
                .HasForeignKey(limit => limit.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
