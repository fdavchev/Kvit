using Kvit.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kvit.Infrastructure.Persistence.Configurations
{
    public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
    {
        public const string NormalizedEmailIndexName = "ix_users_normalized_email";
        public const string NormalizedUserNameIndexName = "ix_users_normalized_user_name";

        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            builder.ToTable("users", table =>
            {
                table.HasCheckConstraint("ck_users_language", "language IN ('en', 'mk')");
                table.HasCheckConstraint("ck_users_display_name_not_empty", "char_length(display_name) >= 1");
                table.HasCheckConstraint("ck_users_time_zone_not_empty", "char_length(time_zone) >= 1");
                table.HasCheckConstraint("ck_users_created_at_set", "created_at > '0002-01-01 00:00:00+00'");
            });

            builder.HasIndex(user => user.NormalizedEmail).HasDatabaseName(NormalizedEmailIndexName).IsUnique();
            builder.HasIndex(user => user.NormalizedUserName).HasDatabaseName(NormalizedUserNameIndexName);

            builder.Property(user => user.DisplayName).HasMaxLength(60);
            builder.Property(user => user.IsTimeZoneManual).HasDefaultValue(false);
            builder.Property(user => user.LockoutCount).HasDefaultValue(0);
            builder.Property(user => user.MustChangePassword).HasDefaultValue(false);
        }
    }
}
