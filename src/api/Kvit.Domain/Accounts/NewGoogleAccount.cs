using Kvit.Domain.Results;

namespace Kvit.Domain.Accounts
{
    public sealed class NewGoogleAccount
    {
        private NewGoogleAccount(string displayName, string email, string timeZone, string language)
        {
            DisplayName = displayName;
            Email = email;
            TimeZone = timeZone;
            Language = language;
        }

        public string DisplayName { get; }

        public string Email { get; }

        public string TimeZone { get; }

        public string Language { get; }

        public static Result<NewGoogleAccount> Create(string displayName, string email, string timeZone, string language)
        {
            Result<string> trimmedDisplayName = AccountRules.ValidateDisplayName(displayName);
            if (!trimmedDisplayName.IsSuccess)
            {
                return Result.Failure<NewGoogleAccount>(trimmedDisplayName.Error, trimmedDisplayName.ErrorCode);
            }

            Result<string> trimmedEmail = AccountRules.ValidateEmail(email);
            if (!trimmedEmail.IsSuccess)
            {
                return Result.Failure<NewGoogleAccount>(trimmedEmail.Error, trimmedEmail.ErrorCode);
            }

            Result[] checks = [AccountRules.ValidateLanguage(language), AccountRules.ValidateTimeZone(timeZone)];
            Result? failed = checks.FirstOrDefault(check => !check.IsSuccess);
            if (failed is not null)
            {
                return Result.Failure<NewGoogleAccount>(failed.Error, failed.ErrorCode);
            }

            return Result.Ok(new NewGoogleAccount(trimmedDisplayName.Value, trimmedEmail.Value, timeZone, language));
        }
    }
}
