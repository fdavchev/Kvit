namespace Kvit.Contracts.Auth
{
    public sealed record LeftBehindGuestsPurge(int PurgedCount, IReadOnlyList<Guid> FailedUserIds);
}
