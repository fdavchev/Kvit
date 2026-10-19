using Kvit.Domain.Results;

namespace Kvit.Domain.Entities
{
    public sealed class CategoryFields
    {
        public const int NameMaxLength = 30;
        public const int EmojiMaxLength = 16;

        public static readonly IReadOnlyList<string> Colors = ["orange", "green", "yellow", "blue", "pink", "purple", "slate", "red", "teal", "gray"];

        private CategoryFields(string name, string emoji, string color)
        {
            Name = name;
            Emoji = emoji;
            Color = color;
        }

        public string Name { get; }

        public string Emoji { get; }

        public string Color { get; }

        public static Result<CategoryFields> Create(string name, string emoji, string color)
        {
            string trimmedName = name.Trim();
            if (trimmedName.Length == 0 || trimmedName.Length > NameMaxLength)
            {
                return Result.Failure<CategoryFields>($"The category name must have 1 to {NameMaxLength} characters.", ResultCodes.CATEGORY_NAME_INVALID);
            }

            if (string.IsNullOrWhiteSpace(emoji) || emoji.Length > EmojiMaxLength)
            {
                return Result.Failure<CategoryFields>($"The category emoji must have 1 to {EmojiMaxLength} characters.", ResultCodes.CATEGORY_EMOJI_INVALID);
            }

            if (!Colors.Contains(color, StringComparer.Ordinal))
            {
                return Result.Failure<CategoryFields>($"'{color}' is not a category color. Use one of: {string.Join(", ", Colors)}.", ResultCodes.CATEGORY_COLOR_INVALID);
            }

            return Result.Ok(new CategoryFields(trimmedName, emoji, color));
        }
    }
}
