using Kvit.Domain.Entities;
using Kvit.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kvit.Infrastructure.Persistence.Configurations
{
    public class DuplicateAnswerConfiguration : IEntityTypeConfiguration<DuplicateAnswer>
    {
        public void Configure(EntityTypeBuilder<DuplicateAnswer> builder)
        {
            builder.ToTable("duplicate_answers", table =>
            {
                table.HasCheckConstraint("ck_duplicate_answers_answer", EnumCheck.OneOf<DuplicateAnswerChoice>("answer"));
            });

            builder.Property(answer => answer.Answer).HasConversion<string>();

            builder.HasIndex(answer => new { answer.UserId, answer.PersonalEntryId, answer.ExpenseId })
                .HasDatabaseName("ix_duplicate_answers_user_id_personal_entry_id_expense_id")
                .IsUnique();

            builder.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(answer => answer.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<PersonalEntry>()
                .WithMany()
                .HasForeignKey(answer => answer.PersonalEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Expense>()
                .WithMany()
                .HasForeignKey(answer => answer.ExpenseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
