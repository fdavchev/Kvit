using Kvit.Domain.Entities;

namespace Kvit.Contracts.Me
{
    public sealed record ExportedMember(
        Guid MemberId,
        string DisplayName,
        bool IsPlainName,
        bool IsYou,
        DateTimeOffset JoinedAt,
        DateTimeOffset? RemovedAt,
        MemberEndKind? EndKind);
}
