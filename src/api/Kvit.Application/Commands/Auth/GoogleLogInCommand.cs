namespace Kvit.Application.Commands.Auth
{
    public sealed record GoogleLogInCommand(string IdToken, string TimeZone);
}
