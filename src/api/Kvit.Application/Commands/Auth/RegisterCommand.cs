namespace Kvit.Application.Commands.Auth
{
    public sealed record RegisterCommand(string DisplayName, string Email, string Password, string TimeZone, string Language);
}
