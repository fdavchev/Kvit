using Kvit.Domain.Entities;

namespace Kvit.Application.Queries.Groups
{
    public sealed record GroupDetailDto(Group Group, int MemberCount);
}
