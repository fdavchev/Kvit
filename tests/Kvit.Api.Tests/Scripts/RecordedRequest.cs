namespace Kvit.Api.Tests.Scripts
{
    public sealed record RecordedRequest(string Method, string PathAndQuery, string Authorization, string Body);
}
