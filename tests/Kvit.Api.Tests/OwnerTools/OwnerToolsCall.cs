using System.Net;

namespace Kvit.Api.Tests.OwnerTools
{
    public sealed record OwnerToolsCall(
        string Name,
        OwnerToolsTarget Target,
        Func<Guid, Guid, string> PathOf,
        Func<HttpClient, Guid, Guid, string, Task<HttpResponseMessage>> SendAsync,
        HttpStatusCode SuccessStatus);
}
