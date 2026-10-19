namespace Kvit.Api.Tests.Groups
{
    public sealed record GuestMember(HttpClient Client, Guid UserId, Guid RowId);
}
