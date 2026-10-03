namespace Kvit.Contracts.Auth
{
    public sealed record GoogleIdentity(string Subject, string Email, bool EmailVerified, string Name, string? PictureUrl);
}
