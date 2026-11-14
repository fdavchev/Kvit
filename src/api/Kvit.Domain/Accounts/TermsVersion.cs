namespace Kvit.Domain.Accounts
{
    public static class TermsVersion
    {
        public const string Current = "2026-11-04";
        public const bool CurrentIsMaterial = false;

        public static bool IsCurrent(string? acceptedVersion)
        {
            return string.Equals(acceptedVersion, Current, StringComparison.Ordinal);
        }

        public static bool UpdateIsMaterial(string? acceptedVersion, string currentVersion, bool currentIsMaterial)
        {
            return currentIsMaterial && !string.Equals(acceptedVersion, currentVersion, StringComparison.Ordinal);
        }
    }
}
