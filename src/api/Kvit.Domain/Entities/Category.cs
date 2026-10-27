using Kvit.Domain.Results;

namespace Kvit.Domain.Entities
{
    public class Category
    {
        public const int LastBuiltInSortOrder = 10;

        private Category()
        {
        }

        public Guid Id { get; private set; }

        public Guid? OwnerUserId { get; private set; }

        public string? Key { get; private set; }

        public string? Name { get; private set; }

        public string Emoji { get; private set; } = string.Empty;

        public string Color { get; private set; } = string.Empty;

        public int SortOrder { get; private set; }

        public DateTimeOffset? ArchivedAt { get; private set; }

        public static Category CreateCustom(Guid ownerUserId, CategoryFields fields, int sortOrder)
        {
            return new Category
            {
                Id = Guid.CreateVersion7(),
                OwnerUserId = ownerUserId,
                Name = fields.Name,
                Emoji = fields.Emoji,
                Color = fields.Color,
                SortOrder = sortOrder,
            };
        }

        public static Result<T> NotFound<T>(Guid categoryId)
        {
            return Result.NotFound<T>($"Category {categoryId} is not one of your own categories.", ResultCodes.CATEGORY_NOT_FOUND);
        }

        public static Result NameTakenMeanwhile()
        {
            return Result.Failure("Another of your active categories got the same name, ignoring letter case, at the same moment; nothing was saved.", ResultCodes.CATEGORY_NAME_TAKEN);
        }

        public void Update(CategoryFields fields)
        {
            Name = fields.Name;
            Emoji = fields.Emoji;
            Color = fields.Color;
        }

        public void Archive(DateTimeOffset archivedAt)
        {
            ArchivedAt ??= archivedAt;
        }

        public void Unarchive()
        {
            ArchivedAt = null;
        }

        public string CustomName()
        {
            return Name ?? throw new InvalidOperationException($"Category {Id} is a built-in category without a name, but only custom categories have one.");
        }

        public bool CanTagGroupExpenses()
        {
            return OwnerUserId is null && ArchivedAt is null;
        }

        public bool CanTagPersonalEntriesOf(Guid userId)
        {
            return (OwnerUserId is null || OwnerUserId == userId) && ArchivedAt is null;
        }

        public bool CanHaveALimitOf(Guid userId)
        {
            return OwnerUserId is null || OwnerUserId == userId;
        }

        public string BuiltInKey()
        {
            return Key ?? throw new InvalidOperationException($"Category {Id} is a custom category without a key, but only built-in categories can be on a group expense.");
        }
    }
}
