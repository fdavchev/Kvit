using Kvit.Domain.Entities;

namespace Kvit.Contracts.OwnerTools
{
    public sealed record OwnerToolsLogRow(
        long Id,
        DateTimeOffset CreatedAt,
        string AdminName,
        AdminActionType Action,
        string TargetKind,
        string TargetLabel,
        string Reason);
}
