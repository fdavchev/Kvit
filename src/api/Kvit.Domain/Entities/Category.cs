namespace Kvit.Domain.Entities
{
    public class Category
    {
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

        public bool CanTagGroupExpenses()
        {
            return OwnerUserId is null && ArchivedAt is null;
        }

        public string BuiltInKey()
        {
            return Key ?? throw new InvalidOperationException($"Category {Id} is a custom category without a key, but only built-in categories can be on a group expense.");
        }
    }
}
