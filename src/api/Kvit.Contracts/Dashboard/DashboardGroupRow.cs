using Kvit.Domain.Entities;

namespace Kvit.Contracts.Dashboard
{
    public sealed record DashboardGroupRow(Guid Id, GroupKind Kind, string Name, string Emoji, GroupStatus Status, IReadOnlyList<CurrencyAmountRow> Balances);
}
