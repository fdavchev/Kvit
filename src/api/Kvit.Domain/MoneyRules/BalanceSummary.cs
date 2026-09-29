namespace Kvit.Domain.MoneyRules
{
    public sealed record BalanceSummary(IReadOnlyDictionary<Currency, IReadOnlyList<MemberBalance>> ByCurrency, bool IsEveryoneKvit);
}
