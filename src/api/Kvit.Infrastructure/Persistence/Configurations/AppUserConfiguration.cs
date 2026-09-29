using Kvit.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kvit.Infrastructure.Persistence.Configurations
{
    public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            builder.ToTable("users", table => table.HasCheckConstraint("ck_users_language", "language IN ('en', 'mk')"));

            builder.HasIndex(user => user.NormalizedEmail).HasDatabaseName("ix_users_normalized_email");
            builder.HasIndex(user => user.NormalizedUserName).HasDatabaseName("ix_users_normalized_user_name");

            builder.Property(user => user.DisplayName).HasMaxLength(60);
            builder.Property(user => user.IsTimeZoneManual).HasDefaultValue(false);
            builder.Property(user => user.LockoutCount).HasDefaultValue(0);
        }
    }
}
