namespace Kvit.Domain.Expenses
{
    public sealed record OneBillCreated(Guid GroupId, Guid ExpenseId);
}
