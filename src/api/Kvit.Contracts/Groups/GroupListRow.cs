using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.Groups
{
    public sealed record GroupListRow(Guid Id, string Name, string Emoji, Currency DefaultCurrency, int MemberCount);
}
