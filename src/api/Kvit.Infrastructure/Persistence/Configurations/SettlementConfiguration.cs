using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kvit.Infrastructure.Persistence.Configurations
{
    public class SettlementConfiguration : IEntityTypeConfiguration<Settlement>
    {
        public const string ClientRequestIdIndexName = "ix_settlements_client_request_id";

        public void Configure(EntityTypeBuilder<Settlement> builder)
        {
            builder.ToTable("settlements", table =>
            {
                table.HasCheckConstraint("ck_settlements_amount_minor_positive", "amount_minor > 0");
                table.HasCheckConstraint("ck_settlements_currency", EnumCheck.OneOf<Currency>("currency"));
                table.HasCheckConstraint("ck_settlements_status", EnumCheck.OneOf<SettlementStatus>("status"));
                table.HasCheckConstraint("ck_settlements_different_members", "from_member_id <> to_member_id");
            });

            builder.Property(settlement => settlement.Currency).HasConversion<string>();
            builder.Property(settlement => settlement.Status).HasConversion<string>();

            builder.HasIndex(settlement => settlement.ClientRequestId).HasDatabaseName(ClientRequestIdIndexName).IsUnique();
            builder.HasIndex(settlement => settlement.GroupId);

            builder.HasOne<Group>()
                .WithMany()
                .HasForeignKey(settlement => settlement.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<GroupMember>()
                .WithMany()
                .HasForeignKey(settlement => settlement.FromMemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<GroupMember>()
                .WithMany()
                .HasForeignKey(settlement => settlement.ToMemberId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
