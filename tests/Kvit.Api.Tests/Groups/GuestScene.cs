namespace Kvit.Api.Tests.Groups
{
    public sealed record GuestScene(GroupSetup Setup, Guid FreeNameRowId, HttpClient Guest);
}
