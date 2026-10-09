namespace Kvit.Contracts.Groups
{
    public sealed record GroupMemberRow(
        Guid Id,
        string Name,
        string DisplayName,
        Guid? UserId,
        bool IsOwner,
        bool IsYou,
        bool IsNameOnly,
        string? ClaimedName,
        string? PictureUrl);
}
