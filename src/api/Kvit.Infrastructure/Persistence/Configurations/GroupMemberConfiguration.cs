using Kvit.Domain.Accounts;
using Kvit.Domain.Entities;
using Kvit.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kvit.Infrastructure.Persistence.Configurations
{
    public class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
    {
        public void Configure(EntityTypeBuilder<GroupMember> builder)
        {
            builder.ToTable("group_members", table =>
            {
                table.HasCheckConstraint("ck_group_members_end_kind", EnumCheck.OneOf<MemberEndKind>("end_kind"));
                table.HasCheckConstraint("ck_group_members_removed_at_with_end_kind", "(removed_at IS NULL) = (end_kind IS NULL)");
                table.HasCheckConstraint("ck_group_members_name_not_empty", "char_length(name) >= 1");
            });

            builder.Property(member => member.EndKind).HasConversion<string>();
            builder.Property(member => member.Name).HasMaxLength(AccountRules.DisplayNameMaxLength);

            builder.HasOne<Group>()
                .WithMany()
                .HasForeignKey(member => member.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(member => member.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(member => member.GroupId);

            builder.HasIndex(member => new { member.GroupId, member.UserId })
                .IsUnique()
                .HasFilter("user_id IS NOT NULL AND removed_at IS NULL");
        }
    }
}
