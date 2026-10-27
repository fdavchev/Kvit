namespace Kvit.Application.Commands.Groups
{
    public sealed record SetCountsInBudgetCommand(Guid GroupId, bool CountsInBudget);
}
