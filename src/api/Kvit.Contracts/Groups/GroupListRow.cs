using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.Groups
{
    public sealed record GroupListRow(Guid Id, GroupKind Kind, string Name, string Emoji, Currency DefaultCurrency, int MemberCount);
}
