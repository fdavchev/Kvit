using Kvit.Contracts.Groups;
using Kvit.Domain.Entities;

namespace Kvit.Application.Queries.Groups
{
    public sealed record GroupListDto(GroupListRow Row, GroupStatus Status, DateTimeOffset? ClosingStartedAt);
}
