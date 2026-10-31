namespace Kvit.Application.Commands.Groups
{
    public sealed record SetGroupPlanCommand(Guid GroupId, long? AmountMinor, string? Currency);
}
