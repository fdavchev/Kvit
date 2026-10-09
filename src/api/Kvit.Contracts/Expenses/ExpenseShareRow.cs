namespace Kvit.Contracts.Expenses
{
    public sealed record ExpenseShareRow(Guid MemberId, string Name, long InputValue, long ShareMinor);
}
