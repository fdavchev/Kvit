namespace Kvit.Contracts.Categories
{
    public sealed record CategoryListResponse(IReadOnlyList<CategoryRow> Categories);
}
