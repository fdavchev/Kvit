using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.Groups
{
    public sealed record GroupResponse(
        Guid Id,
        GroupKind Kind,
        string Name,
        string Emoji,
        Currency DefaultCurrency,
        GroupStatus Status,
        Guid OwnerUserId,
        bool IsOwner,
        int MemberCount,
        string InviteToken,
        DateTimeOffset? ClosingStartedAt,
        DateTimeOffset? ClosingEndsAt,
        IReadOnlyList<ClosingMemberRow> ClosingConfirmedBy,
        IReadOnlyList<ClosingMemberRow> ClosingWaitingFor,
        bool CanStartClosing,
        bool CountsInBudget,
        long? PlanAmountMinor,
        Currency? PlanCurrency);
}
