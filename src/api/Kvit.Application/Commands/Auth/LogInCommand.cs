namespace Kvit.Application.Commands.Auth
{
    public sealed record LogInCommand(string Email, string Password, string TimeZone);
}
