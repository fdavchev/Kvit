using Kvit.Domain.Entities;

namespace Kvit.Infrastructure.Persistence
{
    public static class ExpenseQueries
    {
        public static IQueryable<Guid> MemberIdsInAnyExpenseOf(this AppDbContext context, Guid groupId)
        {
            IQueryable<Expense> expensesOfGroup = context.Expenses.Where(expense => expense.GroupId == groupId);

            return expensesOfGroup
                .Select(expense => expense.PaidByMemberId)
                .Union(context.ExpenseShares
                    .Where(share => expensesOfGroup.Any(expense => expense.Id == share.ExpenseId))
                    .Select(share => share.MemberId));
        }
    }
}
