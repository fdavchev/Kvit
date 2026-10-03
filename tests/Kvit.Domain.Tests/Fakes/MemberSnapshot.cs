using Kvit.Domain.Entities;

namespace Kvit.Domain.Tests.Fakes
{
    public sealed record MemberSnapshot(Guid Id, Guid? UserId, DateTimeOffset? ClaimedAt, DateTimeOffset? RemovedAt, MemberEndKind? EndKind)
    {
        public static MemberSnapshot Of(GroupMember member)
        {
            return new MemberSnapshot(member.Id, member.UserId, member.ClaimedAt, member.RemovedAt, member.EndKind);
        }
    }
}
