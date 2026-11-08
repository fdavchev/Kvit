using System.Globalization;
using Kvit.Domain.Results;

namespace Kvit.Domain.Accounts
{
    public static class AccountDeletionRules
    {
        public const int GraceDays = 30;

        public static Result<DeletionNameChoice> ValidateNameChoice(string? nameChoice)
        {
            foreach (DeletionNameChoice choice in Enum.GetValues<DeletionNameChoice>())
            {
                if (string.Equals(choice.ToString(), nameChoice, StringComparison.Ordinal))
                {
                    return Result.Ok(choice);
                }
            }

            return Result.Failure<DeletionNameChoice>(
                $"The name choice must be one of: {string.Join(", ", Enum.GetNames<DeletionNameChoice>())}, not '{nameChoice}'.",
                ResultCodes.ACCOUNT_DELETE_NAME_CHOICE_INVALID);
        }

        public static DateTimeOffset LatestDueRequestTime(DateTimeOffset now)
        {
            return now.AddDays(-GraceDays);
        }

        public static string RemainingNameOf(DeletionNameChoice choice, string displayName, string language)
        {
            return choice switch
            {
                DeletionNameChoice.KeepName => displayName,
                DeletionNameChoice.FormerMember => FormerMemberName(language),
                _ => throw new ArgumentOutOfRangeException(nameof(choice), choice, "Not a known deletion name choice."),
            };
        }

        public static string FirstFreeName(string wantedName, IReadOnlyCollection<string> takenNames)
        {
            if (!IsTaken(wantedName, takenNames))
            {
                return wantedName;
            }

            for (int number = 2; ; number++)
            {
                string suffix = " " + number.ToString(CultureInfo.InvariantCulture);
                string start = wantedName.Length + suffix.Length > AccountRules.DisplayNameMaxLength
                    ? wantedName[..(AccountRules.DisplayNameMaxLength - suffix.Length)].TrimEnd()
                    : wantedName;
                string candidate = start + suffix;
                if (!IsTaken(candidate, takenNames))
                {
                    return candidate;
                }
            }
        }

        private static string FormerMemberName(string language)
        {
            return language switch
            {
                "en" => "Former member",
                "mk" => "Поранешен член",
                _ => throw new ArgumentOutOfRangeException(nameof(language), language, "Not a language Kvit has a 'Former member' name for."),
            };
        }

        private static bool IsTaken(string name, IReadOnlyCollection<string> takenNames)
        {
            return takenNames.Any(taken => string.Equals(taken, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
