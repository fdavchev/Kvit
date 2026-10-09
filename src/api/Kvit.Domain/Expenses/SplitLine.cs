namespace Kvit.Domain.Expenses
{
    public sealed record SplitLine(Guid MemberId, string Name, long InputValue, long ShareMinor);
}
