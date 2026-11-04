namespace Kvit.Application.Queries.Admin
{
    public sealed record GetAdminStatsQuery(DateOnly? From, DateOnly? To, string? By);
}
