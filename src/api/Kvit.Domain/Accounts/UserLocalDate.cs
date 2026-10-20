namespace Kvit.Domain.Accounts
{
    public static class UserLocalDate
    {
        public static DateOnly Today(DateTimeOffset utcNow, string timeZoneId)
        {
            if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out TimeZoneInfo? timeZone))
            {
                throw new InvalidOperationException($"The time zone '{timeZoneId}' saved for the user is not known on this server, so the user's local date cannot be worked out.");
            }

            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utcNow, timeZone).DateTime);
        }
    }
}
