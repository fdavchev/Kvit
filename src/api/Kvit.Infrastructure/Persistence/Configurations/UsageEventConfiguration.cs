using Kvit.Domain.Entities;
using Kvit.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kvit.Infrastructure.Persistence.Configurations
{
    public class UsageEventConfiguration : IEntityTypeConfiguration<UsageEvent>
    {
        public const string ActiveDayIndexName = "ix_usage_events_user_id_occurred_on";

        public void Configure(EntityTypeBuilder<UsageEvent> builder)
        {
            builder.ToTable("usage_events", table => table.HasCheckConstraint("ck_usage_events_type", EnumCheck.OneOf<UsageEventType>("type")));

            builder.Property(usageEvent => usageEvent.Type).HasConversion<string>();

            builder.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(usageEvent => usageEvent.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(usageEvent => usageEvent.UserId);

            builder.HasIndex(usageEvent => new { usageEvent.UserId, usageEvent.OccurredOn })
                .HasDatabaseName(ActiveDayIndexName)
                .IsUnique()
                .HasFilter($"type = '{UsageEventType.Active}'");
        }
    }
}
