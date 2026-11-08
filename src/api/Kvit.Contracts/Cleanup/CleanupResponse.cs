namespace Kvit.Contracts.Cleanup
{
    public sealed record CleanupResponse(
        int AccountsPurged,
        int GroupsRemoved,
        int ExpensesRemoved,
        int SettlementsRemoved,
        int PersonalEntriesRemoved,
        int UsageEventsDetached);
}
