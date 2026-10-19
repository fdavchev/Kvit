using Kvit.Domain.Results;

namespace Kvit.Domain.Accounts
{
    public sealed class GuestJoinChoice
    {
        private GuestJoinChoice(Guid? claimMemberId, string? newName, string timeZone, string language)
        {
            ClaimMemberId = claimMemberId;
            NewName = newName;
            TimeZone = timeZone;
            Language = language;
        }

        public Guid? ClaimMemberId { get; }

        public string? NewName { get; }

        public string TimeZone { get; }

        public string Language { get; }

        public static Result<GuestJoinChoice> Create(Guid? claimMemberId, string? newName, string timeZone, string language)
        {
            if (claimMemberId.HasValue == (newName is not null))
            {
                return Result.Failure<GuestJoinChoice>("Send either the free name to take or a new name, not both and not neither.", ResultCodes.INVITE_GUEST_CHOICE_INVALID);
            }

            string? trimmedNewName = null;
            if (newName is not null)
            {
                Result<string> validName = AccountRules.ValidateDisplayName(newName);
                if (!validName.IsSuccess)
                {
                    return validName.ToFailure<GuestJoinChoice>();
                }

                trimmedNewName = validName.Value;
            }

            Result[] checks = [AccountRules.ValidateLanguage(language), AccountRules.ValidateTimeZone(timeZone)];
            Result? failed = checks.FirstOrDefault(check => !check.IsSuccess);
            if (failed is not null)
            {
                return failed.ToFailure<GuestJoinChoice>();
            }

            return Result.Ok(new GuestJoinChoice(claimMemberId, trimmedNewName, timeZone, language));
        }
    }
}
