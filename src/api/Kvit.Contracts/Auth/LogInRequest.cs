namespace Kvit.Contracts.Auth
{
    public sealed record LogInRequest(string Email, string Password, string TimeZone);
}
