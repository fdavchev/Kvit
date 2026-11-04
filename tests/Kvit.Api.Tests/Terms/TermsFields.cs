namespace Kvit.Api.Tests.Terms
{
    public sealed record TermsFields(string? Version, bool UpToDate, bool UpdateIsMaterial)
    {
        public static readonly TermsFields Current = new(TermsRequests.CurrentVersion, true, false);

        public static readonly TermsFields NeverAccepted = new(null, false, false);

        public static TermsFields Outdated(string version)
        {
            return new TermsFields(version, false, false);
        }
    }
}
