using Kvit.Domain.Entities;

namespace Kvit.Contracts.OwnerTools
{
    public sealed record OwnerToolsGroupResponse(
        Guid Id,
        string Name,
        string Emoji,
        GroupStatus Status,
        int MemberCount,
        string OwnerName,
        DateTimeOffset CreatedAt,
        bool IsDeleted,
        DateTimeOffset? ClosedByAdminAt);
}
