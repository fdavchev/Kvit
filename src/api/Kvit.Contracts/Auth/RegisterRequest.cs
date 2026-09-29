namespace Kvit.Contracts.Auth
{
    public sealed record RegisterRequest(string DisplayName, string Email, string Password, string TimeZone, string Language);
}
