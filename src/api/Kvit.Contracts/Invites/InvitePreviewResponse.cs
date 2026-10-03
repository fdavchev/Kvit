namespace Kvit.Contracts.Invites
{
    public sealed record InvitePreviewResponse(
        InvitePreviewStatus Status,
        Guid? GroupId,
        string Name,
        string Emoji,
        IReadOnlyList<string> MemberNames,
        IReadOnlyList<UnclaimedNameRow> UnclaimedNames);
}
