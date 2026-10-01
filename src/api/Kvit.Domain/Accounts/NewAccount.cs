using Kvit.Domain.Results;

namespace Kvit.Domain.Accounts
{
    public sealed class NewAccount
    {
        private NewAccount(string displayName, string email, string password, string timeZone, string language)
        {
            DisplayName = displayName;
            Email = email;
            Password = password;
            TimeZone = timeZone;
            Language = language;
        }

        public string DisplayName { get; }

        public string Email { get; }

        public string Password { get; }

        public string TimeZone { get; }

        public string Language { get; }

        public static Result<NewAccount> Create(string displayName, string email, string password, string timeZone, string language)
        {
            Result<string> trimmedDisplayName = AccountRules.ValidateDisplayName(displayName);
            if (!trimmedDisplayName.IsSuccess)
            {
                return Result.Failure<NewAccount>(trimmedDisplayName.Error, trimmedDisplayName.ErrorCode);
            }

            Result<string> trimmedEmail = AccountRules.ValidateEmail(email);
            if (!trimmedEmail.IsSuccess)
            {
                return Result.Failure<NewAccount>(trimmedEmail.Error, trimmedEmail.ErrorCode);
            }

            Result[] checks = [AccountRules.ValidatePassword(password), AccountRules.ValidateLanguage(language), AccountRules.ValidateTimeZone(timeZone)];
            Result? failed = checks.FirstOrDefault(check => !check.IsSuccess);
            if (failed is not null)
            {
                return Result.Failure<NewAccount>(failed.Error, failed.ErrorCode);
            }

            return Result.Ok(new NewAccount(trimmedDisplayName.Value, trimmedEmail.Value, password, timeZone, language));
        }
    }
}
