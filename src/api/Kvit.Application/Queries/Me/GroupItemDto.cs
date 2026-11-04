namespace Kvit.Application.Queries.Me
{
    public sealed record GroupItemDto<TItem>(Guid GroupId, TItem Item);
}
