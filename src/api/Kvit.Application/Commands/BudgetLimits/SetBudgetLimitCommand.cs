namespace Kvit.Application.Commands.BudgetLimits
{
    public sealed record SetBudgetLimitCommand(Guid? CategoryId, long? LimitMinor);
}
