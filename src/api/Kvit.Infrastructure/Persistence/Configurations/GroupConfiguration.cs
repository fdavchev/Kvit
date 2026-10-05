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
            });

            builder.Property(group => group.Kind).HasConversion<string>();
            builder.Property(group => group.Status).HasConversion<string>();
            builder.Property(group => group.DefaultCurrency).HasConversion<string>();
            builder.Property(group => group.Name).HasMaxLength(GroupSettings.NameMaxLength);

            builder.HasIndex(group => group.InviteToken).IsUnique();

            builder.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(group => group.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
