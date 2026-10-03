namespace Kvit.Contracts.Auth
{
    public sealed record GoogleSignUpRequest(string IdToken, string DisplayName, string TimeZone, string Language);
}
