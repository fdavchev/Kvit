namespace Kvit.Application.Commands.Auth
{
    public sealed record GoogleSignUpCommand(string IdToken, string DisplayName, string TimeZone, string Language);
}
