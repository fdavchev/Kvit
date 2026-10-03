using System.Net.Mail;
using System.Text.RegularExpressions;
using Kvit.Domain.Results;

namespace Kvit.Domain.Accounts
{
    public static partial class AccountRules
    {
        public const int DisplayNameMaxLength = 60;
        public const int EmailMaxLength = 254;
        public const int PasswordMinLength = 8;

        private static readonly string[] SupportedLanguages = ["en", "mk"];

        public static Result<string> ValidateDisplayName(string displayName)
        {
            string trimmed = displayName.Trim();
            if (trimmed.Length == 0 || trimmed.Length > DisplayNameMaxLength)
            {
                return Result.Failure<string>($"The name must have 1 to {DisplayNameMaxLength} characters.", ResultCodes.AUTH_DISPLAY_NAME_INVALID);
            }

            return Result.Ok(trimmed);
        }

        public static Result<string> ValidateEmail(string email)
        {
            string trimmed = email.Trim();
            bool isPlainAddress = trimmed.Length > 0
                && trimmed.Length <= EmailMaxLength
                && !trimmed.Any(char.IsWhiteSpace)
                && MailAddress.TryCreate(trimmed, out MailAddress? parsed)
                && string.Equals(parsed.Address, trimmed, StringComparison.Ordinal);

            if (!isPlainAddress)
            {
                return Result.Failure<string>("The email address is not valid.", ResultCodes.AUTH_EMAIL_INVALID);
            }

            return Result.Ok(trimmed);
        }

        public static Result ValidatePassword(string password)
        {
            if (password.Length < PasswordMinLength || !password.Any(char.IsUpper) || !password.Any(char.IsDigit))
            {
                return Result.Failure($"The password needs at least {PasswordMinLength} characters, one capital letter and one number.", ResultCodes.AUTH_PASSWORD_TOO_WEAK);
            }

            return Result.Ok();
        }

        public static Result ValidateNewPassword(string currentPassword, string newPassword)
        {
            Result passwordRule = ValidatePassword(newPassword);
            if (!passwordRule.IsSuccess)
            {
                return passwordRule;
            }

            if (string.Equals(currentPassword, newPassword, StringComparison.Ordinal))
            {
                return Result.Failure("The new password must be different from the current one.", ResultCodes.AUTH_PASSWORD_UNCHANGED);
            }

            return Result.Ok();
        }

        public static Result ValidateGoogleEmailVerified(bool isEmailVerified)
        {
            if (!isEmailVerified)
            {
                return Result.Failure("Google has not verified the email address of this Google account.", ResultCodes.AUTH_GOOGLE_EMAIL_NOT_VERIFIED);
            }

            return Result.Ok();
        }

        public static Result ValidateLanguage(string language)
        {
            if (!SupportedLanguages.Contains(language, StringComparer.Ordinal))
            {
                return Result.Failure($"The language must be one of: {string.Join(", ", SupportedLanguages)}.", ResultCodes.LANGUAGE_INVALID);
            }

            return Result.Ok();
        }

        public static Result ValidateTimeZone(string timeZone)
        {
            if (!TimeZoneShape().IsMatch(timeZone) || !TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out _))
            {
                return Result.Failure($"'{timeZone}' is not a known time zone.", ResultCodes.TIME_ZONE_INVALID);
            }

            return Result.Ok();
        }

        [GeneratedRegex("^[A-Za-z_]+(/[A-Za-z0-9_+-]+)*$", RegexOptions.CultureInvariant)]
        private static partial Regex TimeZoneShape();
    }
}
