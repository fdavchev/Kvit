using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kvit.Infrastructure.Persistence.Configurations
{
    public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
    {
        public const string ClientRequestIdIndexName = "ix_expenses_client_request_id";

        public void Configure(EntityTypeBuilder<Expense> builder)
        {
            builder.ToTable("expenses", table =>
            {
                table.HasCheckConstraint("ck_expenses_amount_minor_positive", "amount_minor > 0");
                table.HasCheckConstraint("ck_expenses_currency", EnumCheck.OneOf<Currency>("currency"));
                table.HasCheckConstraint("ck_expenses_split_type", EnumCheck.OneOf<SplitType>("split_type"));
                table.HasCheckConstraint("ck_expenses_title_length", $"char_length(title) BETWEEN 1 AND {ExpenseFields.TitleMaxLength}");
                table.HasCheckConstraint("ck_expenses_note_length", $"char_length(note) BETWEEN 1 AND {ExpenseFields.NoteMaxLength}");
                table.HasCheckConstraint("ck_expenses_mkd_per_eur_positive", "mkd_per_eur > 0");
            });

            builder.Property(expense => expense.Currency).HasConversion<string>();
            builder.Property(expense => expense.SplitType).HasConversion<string>();
            builder.Property(expense => expense.MkdPerEur).HasPrecision(ExchangeRate.MkdPerEurPrecision, ExchangeRate.MkdPerEurDecimals);

            builder.HasIndex(expense => expense.ClientRequestId).HasDatabaseName(ClientRequestIdIndexName).IsUnique();

            builder.HasOne<Group>()
                .WithMany()
                .HasForeignKey(expense => expense.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<GroupMember>()
                .WithMany()
                .HasForeignKey(expense => expense.PaidByMemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Category>()
                .WithMany()
                .HasForeignKey(expense => expense.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(expense => expense.Shares)
                .WithOne()
                .HasForeignKey(share => share.ExpenseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
