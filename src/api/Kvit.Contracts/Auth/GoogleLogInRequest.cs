namespace Kvit.Contracts.Auth
{
    public sealed record GoogleLogInRequest(string IdToken, string TimeZone);
}
