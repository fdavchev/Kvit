namespace Kvit.Contracts.OwnerTools
{
    public sealed record OwnerToolsPersonRow(
        Guid Id,
        string DisplayName,
        string? Email,
        bool IsGuest,
        bool IsSuspended,
        bool IsAdmin,
        bool DeletionPending,
        bool IsDeleted,
        DateTimeOffset CreatedAt,
        int GroupCount);
}
