namespace Kvit.Api.Tests.Terms
{
    public sealed record StoredTerms(string? Version, DateTimeOffset? AcceptedAt)
    {
        public static readonly StoredTerms None = new(null, null);
    }
}
