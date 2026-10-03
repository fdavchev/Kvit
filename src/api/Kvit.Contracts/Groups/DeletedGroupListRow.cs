using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.Groups
{
    public sealed record DeletedGroupListRow(
        Guid Id,
        string Name,
        string Emoji,
        Currency DefaultCurrency,
        int MemberCount,
        DateTimeOffset DeletedAt,
        DateTimeOffset RestorableUntil);
}
