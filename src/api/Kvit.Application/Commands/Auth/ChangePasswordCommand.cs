namespace Kvit.Application.Commands.Auth
{
    public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword);
}
