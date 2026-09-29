using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kvit.Infrastructure.Persistence.Configurations
{
    public class RoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
    {
        public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> builder)
        {
            builder.ToTable("role_claims");

            builder.HasOne<IdentityRole<Guid>>()
                .WithMany()
                .HasForeignKey(claim => claim.RoleId)
                .HasConstraintName("fk_role_claims_roles_role_id");
        }
    }
}
