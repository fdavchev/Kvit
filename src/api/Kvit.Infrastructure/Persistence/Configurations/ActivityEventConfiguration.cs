using Kvit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kvit.Infrastructure.Persistence.Configurations
{
    public class ActivityEventConfiguration : IEntityTypeConfiguration<ActivityEvent>
    {
        public void Configure(EntityTypeBuilder<ActivityEvent> builder)
        {
            builder.ToTable("activity_events", table => table.HasCheckConstraint("ck_activity_events_type", EnumCheck.OneOf<ActivityEventType>("type")));

            builder.Property(activityEvent => activityEvent.Type).HasConversion<string>();
            builder.Property(activityEvent => activityEvent.Changes).HasColumnType("jsonb");
            builder.Property(activityEvent => activityEvent.Data).HasColumnType("jsonb");

            builder.HasOne<Group>()
                .WithMany()
                .HasForeignKey(activityEvent => activityEvent.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(activityEvent => new { activityEvent.GroupId, activityEvent.CreatedAt })
                .IsDescending(false, true);
        }
    }
}
