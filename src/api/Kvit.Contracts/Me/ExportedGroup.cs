using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.Me
{
    public sealed record ExportedGroup(
        Guid Id,
        string Name,
        string Emoji,
        Currency DefaultCurrency,
        GroupStatus Status,
        bool IsOwner,
        bool CountsInBudget,
        ExportedMembership Membership,
        IReadOnlyList<ExportedMember> Members,
        IReadOnlyList<ExportedExpense> Expenses,
        IReadOnlyList<ExportedPayment> Payments);
}
