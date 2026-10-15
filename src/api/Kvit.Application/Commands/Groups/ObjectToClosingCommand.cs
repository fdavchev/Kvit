namespace Kvit.Application.Commands.Groups
{
    public sealed record ObjectToClosingCommand(Guid GroupId, string? Reason);
}
