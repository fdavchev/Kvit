namespace Kvit.Api.Tests.Auth
{
    public sealed record RegistrationForm(string DisplayName, string Email, string Password, string TimeZone, string Language)
    {
        public const string ValidPassword = "Sunce2026";

        public static RegistrationForm Valid()
        {
            return new RegistrationForm("Ana", UniqueEmail(), ValidPassword, "Europe/Skopje", "en");
        }

        public static string UniqueEmail()
        {
            return $"{Guid.NewGuid():N}@example.com";
        }
    }
}
