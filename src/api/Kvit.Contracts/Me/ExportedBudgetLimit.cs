namespace Kvit.Contracts.Me
{
    public sealed record ExportedBudgetLimit(Guid? CategoryId, DateOnly EffectiveFrom, long? LimitMinor, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
}
