using Kvit.Domain.Entities;
using Kvit.Infrastructure.Auth;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Infrastructure.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options)
        : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options), IDataProtectionKeyContext
    {
        public DbSet<UsageEvent> UsageEvents => Set<UsageEvent>();

        public DbSet<Group> Groups => Set<Group>();

        public DbSet<GroupMember> GroupMembers => Set<GroupMember>();

        public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();

        public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
