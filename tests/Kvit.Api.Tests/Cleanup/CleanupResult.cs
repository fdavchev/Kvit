namespace Kvit.Api.Tests.Cleanup
{
    public sealed record CleanupResult(
        int AccountsPurged,
        int GroupsRemoved,
        int ExpensesRemoved,
        int SettlementsRemoved,
        int PersonalEntriesRemoved,
        int UsageEventsDetached)
    {
        public static readonly CleanupResult Nothing = new(0, 0, 0, 0, 0, 0);

        public static readonly CleanupResult OneOfEveryKind = new(1, 1, 1, 1, 1, 1);
    }
}
