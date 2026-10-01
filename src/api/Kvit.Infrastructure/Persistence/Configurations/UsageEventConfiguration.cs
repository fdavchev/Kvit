using Kvit.Domain.Entities;
using Kvit.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kvit.Infrastructure.Persistence.Configurations
{
    public class UsageEventConfiguration : IEntityTypeConfiguration<UsageEvent>
    {
        public void Configure(EntityTypeBuilder<UsageEvent> builder)
        {
            string knownTypes = string.Join(", ", Enum.GetNames<UsageEventType>().Select(name => $"'{name}'"));
            builder.ToTable("usage_events", table => table.HasCheckConstraint("ck_usage_events_type", $"type IN ({knownTypes})"));

            builder.Property(usageEvent => usageEvent.Type).HasConversion<string>();

            builder.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(usageEvent => usageEvent.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(usageEvent => usageEvent.UserId);

            builder.HasIndex(usageEvent => new { usageEvent.UserId, usageEvent.OccurredOn })
                .IsUnique()
                .HasFilter($"type = '{UsageEventType.Active}'");
        }
    }
}
