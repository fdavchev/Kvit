using Kvit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kvit.Infrastructure.Persistence.Configurations
{
    public class ExpenseShareConfiguration : IEntityTypeConfiguration<ExpenseShare>
    {
        public void Configure(EntityTypeBuilder<ExpenseShare> builder)
        {
            builder.ToTable("expense_shares", table =>
            {
                table.HasCheckConstraint("ck_expense_shares_input_value_not_negative", "input_value >= 0");
                table.HasCheckConstraint("ck_expense_shares_share_minor_not_negative", "share_minor >= 0");
            });

            builder.HasKey(share => new { share.ExpenseId, share.MemberId });

            builder.HasOne<GroupMember>()
                .WithMany()
                .HasForeignKey(share => share.MemberId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
