using Kvit.Domain.Entities;

namespace Kvit.Application.Queries.Dashboard
{
    public sealed record DashboardGroupDto(
        Guid Id,
        GroupKind Kind,
        string Name,
        string Emoji,
        GroupStatus Status,
        DateTimeOffset? ClosingStartedAt,
        Guid OwnerUserId,
        string OwnerName,
        Guid CallerMemberId,
        DateTimeOffset CallerJoinedAt);
}
