namespace Kvit.Api.Tests.Persistence
{
    public sealed record StoredShare(Guid MemberId, long InputValue, long ShareMinor);
}
