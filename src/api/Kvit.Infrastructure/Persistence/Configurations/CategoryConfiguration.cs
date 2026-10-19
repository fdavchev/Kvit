using Kvit.Domain.Entities;
using Kvit.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kvit.Infrastructure.Persistence.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("categories", table =>
            {
                table.HasCheckConstraint(
                    "ck_categories_built_in_or_custom",
                    "(owner_user_id IS NULL AND key IS NOT NULL AND name IS NULL) OR (owner_user_id IS NOT NULL AND key IS NULL AND name IS NOT NULL)");
                table.HasCheckConstraint("ck_categories_emoji_not_empty", "char_length(emoji) >= 1");
                table.HasCheckConstraint("ck_categories_color", $"color IN ({string.Join(", ", CategoryFields.Colors.Select(color => $"'{color}'"))})");
            });

            builder.HasIndex(category => category.Key).IsUnique();

            builder.HasIndex(category => category.OwnerUserId);

            builder.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(category => category.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasData(
                BuiltIn("c47e9a10-5f3b-4d2e-9a61-000000000001", "food", "\U0001F37D️", "orange", 1),
                BuiltIn("c47e9a10-5f3b-4d2e-9a61-000000000002", "groceries", "\U0001F6D2", "green", 2),
                BuiltIn("c47e9a10-5f3b-4d2e-9a61-000000000003", "transport", "\U0001F695", "yellow", 3),
                BuiltIn("c47e9a10-5f3b-4d2e-9a61-000000000004", "accommodation", "\U0001F3E8", "blue", 4),
                BuiltIn("c47e9a10-5f3b-4d2e-9a61-000000000005", "fun", "\U0001F389", "pink", 5),
                BuiltIn("c47e9a10-5f3b-4d2e-9a61-000000000006", "shopping", "\U0001F6CD️", "purple", 6),
                BuiltIn("c47e9a10-5f3b-4d2e-9a61-000000000007", "bills", "\U0001F9FE", "slate", 7),
                BuiltIn("c47e9a10-5f3b-4d2e-9a61-000000000008", "health", "\U0001F48A", "red", 8),
                BuiltIn("c47e9a10-5f3b-4d2e-9a61-000000000009", "gifts", "\U0001F381", "teal", 9),
                BuiltIn("c47e9a10-5f3b-4d2e-9a61-000000000010", "other", "\U0001F4E6", "gray", 10));
        }

        private static object BuiltIn(string id, string key, string emoji, string color, int sortOrder)
        {
            return new
            {
                Id = Guid.Parse(id),
                Key = key,
                Emoji = emoji,
                Color = color,
                SortOrder = sortOrder,
            };
        }
    }
}
