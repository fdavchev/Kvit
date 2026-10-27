namespace Kvit.Infrastructure.Persistence
{
    public static class BudgetShareQueries
    {
        public static IQueryable<CountedShare> CountedSharesOf(this AppDbContext context, Guid userId)
        {
            return context.ExpenseShares
                .Where(share => share.CountsInBudget && share.ShareMinor > 0)
                .Join(context.GroupMembers.Where(member => member.UserId == userId), share => share.MemberId, member => member.Id, (share, member) => share)
                .Join(
                    context.Expenses.Where(expense => expense.DeletedAt == null),
                    share => share.ExpenseId,
                    expense => expense.Id,
                    (share, expense) => new { Share = share, Expense = expense })
                .Join(
                    context.Groups.Where(group => group.DeletedAt == null),
                    row => row.Expense.GroupId,
                    group => group.Id,
                    (row, group) => new CountedShare { Share = row.Share, Expense = row.Expense, Group = group });
        }
    }
}
