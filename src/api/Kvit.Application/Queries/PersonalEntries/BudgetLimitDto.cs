namespace Kvit.Application.Queries.PersonalEntries
{
    public sealed record BudgetLimitDto(Guid? CategoryId, DateOnly EffectiveFrom, long? LimitMinor);
}
