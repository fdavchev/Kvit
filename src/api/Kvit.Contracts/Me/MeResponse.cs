namespace Kvit.Contracts.Me
{
    public sealed record MeResponse(Guid Id, string DisplayName, string Email, string Language, string TimeZone);
}
