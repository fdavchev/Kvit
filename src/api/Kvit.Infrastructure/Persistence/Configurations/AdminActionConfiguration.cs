using Kvit.Domain.Entities;
using Kvit.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kvit.Infrastructure.Persistence.Configurations
{
    public class AdminActionConfiguration : IEntityTypeConfiguration<AdminAction>
    {
        public void Configure(EntityTypeBuilder<AdminAction> builder)
        {
            builder.ToTable("admin_actions", table =>
            {
                table.HasCheckConstraint("ck_admin_actions_action", EnumCheck.OneOf<AdminActionType>("action"));
                table.HasCheckConstraint("ck_admin_actions_reason_not_empty", "char_length(reason) >= 1");
                table.HasCheckConstraint("ck_admin_actions_one_target", "(target_user_id IS NULL) <> (target_group_id IS NULL)");
            });

            builder.Property(adminAction => adminAction.Action).HasConversion<string>().HasMaxLength(30);
            builder.Property(adminAction => adminAction.TargetLabel).HasMaxLength(AdminAction.TargetLabelMaxLength);
            builder.Property(adminAction => adminAction.Reason).HasMaxLength(AdminAction.ReasonMaxLength);

            builder.HasIndex(adminAction => adminAction.CreatedAt);

            builder.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(adminAction => adminAction.AdminUserId)
                .HasConstraintName("fk_admin_actions_users_admin_user_id")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(adminAction => adminAction.TargetUserId)
                .HasConstraintName("fk_admin_actions_users_target_user_id")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
