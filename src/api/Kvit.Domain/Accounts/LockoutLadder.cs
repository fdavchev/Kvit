namespace Kvit.Domain.Accounts
{
    public static class LockoutLadder
    {
        public const int FailedTriesBeforeLock = 5;

        public static TimeSpan LockDurationFor(int lockoutCount)
        {
            return lockoutCount switch
            {
                < 1 => throw new ArgumentOutOfRangeException(nameof(lockoutCount), lockoutCount, "A lock duration is only asked for once the account has been locked at least once."),
                1 => TimeSpan.FromMinutes(5),
                2 => TimeSpan.FromMinutes(10),
                _ => TimeSpan.FromMinutes(15),
            };
        }
    }
}
