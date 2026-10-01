namespace Kvit.Contracts.Auth
{
    public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
}
