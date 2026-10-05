namespace Kvit.Application.Commands.Groups
{
    public sealed record LetBackInCommand(Guid GroupId, Guid MemberId);
}
