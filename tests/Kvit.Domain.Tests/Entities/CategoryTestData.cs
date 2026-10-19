using Kvit.Domain.Entities;
using Kvit.Domain.Results;

namespace Kvit.Domain.Tests.Entities
{
    public static class CategoryTestData
    {
        public const string ValidName = "Pets";
        public const string ValidEmoji = "\U0001F43E";
        public const string ValidColor = "teal";
        public const int FirstCustomSortOrder = 11;

        public static readonly Guid OwnerId = Guid.Parse("0199f0a1-b2c3-7d4e-8f5a-6b7c8d9e0f10");
        public static readonly Guid OtherOwnerId = Guid.Parse("0199f0a1-b2c3-7d4e-8f5a-6b7c8d9e0f11");
        public static readonly DateTimeOffset Moment = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

        public static readonly string[] Colors = ["orange", "green", "yellow", "blue", "pink", "purple", "slate", "red", "teal", "gray"];

        public static CategoryFields Fields(string name = ValidName, string emoji = ValidEmoji, string color = ValidColor)
        {
            Result<CategoryFields> fields = CategoryFields.Create(name, emoji, color);

            return fields.Value;
        }

        public static Category NewCustom(string name = ValidName, int sortOrder = FirstCustomSortOrder)
        {
            return Category.CreateCustom(OwnerId, Fields(name), sortOrder);
        }

        public static Category NewArchived(string name, int sortOrder, DateTimeOffset archivedAt)
        {
            Category category = NewCustom(name, sortOrder);
            category.Archive(archivedAt);

            return category;
        }
    }
}
