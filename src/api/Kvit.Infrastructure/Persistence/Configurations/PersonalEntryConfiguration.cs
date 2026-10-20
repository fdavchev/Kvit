using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;
using Kvit.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kvit.Infrastructure.Persistence.Configurations
{
    public class PersonalEntryConfiguration : IEntityTypeConfiguration<PersonalEntry>
    {
        public const string ClientRequestIdIndexName = "ix_personal_entries_client_request_id";
        public const string RecurringIncomeDateIndexName = "ix_personal_entries_recurring_income_id_entry_date";

        public void Configure(EntityTypeBuilder<PersonalEntry> builder)
        {
            builder.ToTable("personal_entries", table =>
            {
                table.HasCheckConstraint("ck_personal_entries_kind", EnumCheck.OneOf<PersonalEntryKind>("kind"));
                table.HasCheckConstraint("ck_personal_entries_amount_minor_positive", "amount_minor > 0");
                table.HasCheckConstraint("ck_personal_entries_currency", EnumCheck.OneOf<Currency>("currency"));
                table.HasCheckConstraint("ck_personal_entries_note_length", $"char_length(note) BETWEEN 1 AND {ExpenseFields.NoteMaxLength}");
                table.HasCheckConstraint("ck_personal_entries_mkd_per_eur_positive", "mkd_per_eur > 0");
                table.HasCheckConstraint("ck_personal_entries_spending_has_category", $"kind <> '{nameof(PersonalEntryKind.Spending)}' OR category_id IS NOT NULL");
            });

            builder.Property(entry => entry.Kind).HasConversion<string>();
            builder.Property(entry => entry.Currency).HasConversion<string>();
            builder.Property(entry => entry.MkdPerEur).HasPrecision(ExchangeRate.MkdPerEurPrecision, ExchangeRate.MkdPerEurDecimals);

            builder.HasIndex(entry => entry.ClientRequestId).HasDatabaseName(ClientRequestIdIndexName).IsUnique();
            builder.HasIndex(entry => new { entry.UserId, entry.EntryDate });
            builder.HasIndex(entry => new { entry.RecurringIncomeId, entry.EntryDate }).HasDatabaseName(RecurringIncomeDateIndexName).IsUnique();

            builder.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(entry => entry.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Category>()
                .WithMany()
                .HasForeignKey(entry => entry.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<RecurringIncome>()
                .WithMany()
                .HasForeignKey(entry => entry.RecurringIncomeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
