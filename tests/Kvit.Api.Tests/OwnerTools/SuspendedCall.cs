namespace Kvit.Api.Tests.OwnerTools
{
    public sealed record SuspendedCall(string Name, bool NeedsAdmin, Func<HttpClient, Task<HttpResponseMessage>> SendAsync);
}
