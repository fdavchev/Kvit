using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;

namespace Kvit.Application.Queries.Me
{
    public sealed record MyGroupMembershipDto(
        GroupMember Member,
        string GroupName,
        string GroupEmoji,
        Currency GroupDefaultCurrency,
        GroupStatus GroupStatus,
        DateTimeOffset? GroupClosingStartedAt,
        bool IsOwner);
}
