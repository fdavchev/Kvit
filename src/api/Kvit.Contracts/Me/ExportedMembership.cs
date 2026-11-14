using Kvit.Domain.Entities;

namespace Kvit.Contracts.Me
{
    public sealed record ExportedMembership(Guid MemberId, DateTimeOffset JoinedAt, DateTimeOffset? RemovedAt, MemberEndKind? EndKind);
}
