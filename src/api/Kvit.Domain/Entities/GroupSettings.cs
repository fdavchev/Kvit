using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;

namespace Kvit.Domain.Entities
{
    public sealed class GroupSettings
    {
        public const int NameMaxLength = 60;
        public const int EmojiMaxLength = 32;

        private GroupSettings(string name, string emoji, Currency defaultCurrency)
        {
            Name = name;
            Emoji = emoji;
            DefaultCurrency = defaultCurrency;
        }

        public string Name { get; }

        public string Emoji { get; }

        public Currency DefaultCurrency { get; }

        public static Result<GroupSettings> Create(string name, string emoji, string currency)
        {
            string trimmedName = name.Trim();
            if (trimmedName.Length == 0 || trimmedName.Length > NameMaxLength)
            {
                return Result.Failure<GroupSettings>($"The group name must have 1 to {NameMaxLength} characters.", ResultCodes.GROUP_NAME_INVALID);
            }

            if (string.IsNullOrWhiteSpace(emoji) || emoji.Length > EmojiMaxLength)
            {
                return Result.Failure<GroupSettings>($"The group emoji must have 1 to {EmojiMaxLength} characters.", ResultCodes.GROUP_EMOJI_INVALID);
            }

            string[] currencyNames = Enum.GetNames<Currency>();
            if (!currencyNames.Contains(currency, StringComparer.Ordinal))
            {
                return Result.Failure<GroupSettings>($"'{currency}' is not a supported currency. Use one of: {string.Join(", ", currencyNames)}.", ResultCodes.GROUP_CURRENCY_INVALID);
            }

            return Result.Ok(new GroupSettings(trimmedName, emoji, Enum.Parse<Currency>(currency)));
        }
    }
}
