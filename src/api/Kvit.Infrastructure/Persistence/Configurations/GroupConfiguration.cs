using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;
using Kvit.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kvit.Infrastructure.Persistence.Configurations
{
    public class GroupConfiguration : IEntityTypeConfiguration<Group>
    {
        public void Configure(EntityTypeBuilder<Group> builder)
        {
            builder.ToTable("groups", table =>
            {
                table.HasCheckConstraint("ck_groups_kind", EnumCheck.OneOf<GroupKind>("kind"));
                table.HasCheckConstraint("ck_groups_status", EnumCheck.OneOf<GroupStatus>("status"));
                table.HasCheckConstraint("ck_groups_default_currency", EnumCheck.OneOf<Currency>("default_currency"));
                table.HasCheckConstraint("ck_groups_name_not_empty", "char_length(name) >= 1");
                table.HasCheckConstraint("ck_groups_plan_both_or_neither", "(plan_amount_minor IS NULL) = (plan_currency IS NULL)");
                table.HasCheckConstraint("ck_groups_plan_amount_minor", $"plan_amount_minor > 0 AND plan_amount_minor <= {ExpenseFields.AmountMaxMinorUnits}");
                table.HasCheckConstraint("ck_groups_plan_currency", EnumCheck.OneOf<Currency>("plan_currency"));
                table.HasCheckConstraint("ck_groups_plan_mkd_whole_denars", $"plan_currency <> '{Currency.MKD}' OR plan_amount_minor % {CurrencyRules.StepMinorUnits(Currency.MKD)} = 0");
            });

            builder.Property(group => group.Kind).HasConversion<string>();
            builder.Property(group => group.Status).HasConversion<string>();
            builder.Property(group => group.DefaultCurrency).HasConversion<string>();
            builder.Property(group => group.PlanCurrency).HasConversion<string>();
            builder.Property(group => group.Name).HasMaxLength(GroupSettings.NameMaxLength);

            builder.HasIndex(group => group.InviteToken).IsUnique();

            builder.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(group => group.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
