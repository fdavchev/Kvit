using Kvit.Domain.Entities;
using Kvit.Domain.MoneyRules;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Infrastructure.Persistence
{
    public static class BalanceQueries
    {
        public static async Task<GroupMoney> GroupMoneyOfAsync(this AppDbContext context, Guid groupId, CancellationToken cancellationToken)
        {
            List<Guid> memberIds = await context.NamedGroupMembers(groupId)
                .AsNoTracking()
                .InJoiningOrder()
                .Select(named => named.Member.Id)
                .ToListAsync(cancellationToken);
            List<BalanceExpense> expenses = await context.CountedBalanceExpensesOfAsync(groupId, cancellationToken);
            List<BalanceSettlement> settlements = await context.CountedBalanceSettlementsOfAsync(groupId, cancellationToken);

            return new GroupMoney(memberIds, expenses, settlements);
        }

        public static async Task<Dictionary<Guid, GroupMoney>> GroupMoneyOfGroupsAsync(this AppDbContext context, IReadOnlyCollection<Guid> groupIds, CancellationToken cancellationToken)
        {
            List<GroupMember> members = await context.GroupMembers
                .AsNoTracking()
                .Where(member => groupIds.Contains(member.GroupId))
                .OrderBy(member => member.JoinedAt)
                .ThenBy(member => member.Id)
                .ToListAsync(cancellationToken);
            List<Expense> expenses = await CountedExpenses(context)
                .Where(expense => groupIds.Contains(expense.GroupId))
                .ToListAsync(cancellationToken);
            List<Settlement> settlements = await CountedSettlements(context)
                .Where(settlement => groupIds.Contains(settlement.GroupId))
                .ToListAsync(cancellationToken);
            ILookup<Guid, Guid> memberIdsByGroup = members.ToLookup(member => member.GroupId, member => member.Id);
            ILookup<Guid, Expense> expensesByGroup = expenses.ToLookup(expense => expense.GroupId);
            ILookup<Guid, Settlement> settlementsByGroup = settlements.ToLookup(settlement => settlement.GroupId);

            return groupIds.Distinct().ToDictionary(
                groupId => groupId,
                groupId => new GroupMoney(
                    [.. memberIdsByGroup[groupId]],
                    [.. expensesByGroup[groupId].Select(BalanceExpenseOf)],
                    [.. settlementsByGroup[groupId].Select(BalanceSettlementOf)]));
        }

        public static async Task<List<BalanceExpense>> CountedBalanceExpensesOfAsync(this AppDbContext context, Guid groupId, CancellationToken cancellationToken)
        {
            List<Expense> expenses = await CountedExpenses(context)
                .Where(expense => expense.GroupId == groupId)
                .ToListAsync(cancellationToken);

            return [.. expenses.Select(BalanceExpenseOf)];
        }

        public static async Task<List<BalanceSettlement>> CountedBalanceSettlementsOfAsync(this AppDbContext context, Guid groupId, CancellationToken cancellationToken)
        {
            List<Settlement> settlements = await CountedSettlements(context)
                .Where(settlement => settlement.GroupId == groupId)
                .ToListAsync(cancellationToken);

            return [.. settlements.Select(BalanceSettlementOf)];
        }

        private static IQueryable<Expense> CountedExpenses(AppDbContext context)
        {
            return context.Expenses
                .AsNoTracking()
                .Include(expense => expense.Shares)
                .Where(expense => expense.DeletedAt == null);
        }

        private static IQueryable<Settlement> CountedSettlements(AppDbContext context)
        {
            return context.Settlements
                .AsNoTracking()
                .Where(settlement => settlement.DeletedAt == null);
        }

        private static BalanceExpense BalanceExpenseOf(Expense expense)
        {
            List<SplitShare> shares = [.. expense.Shares.Select(share => new SplitShare(
                share.MemberId,
                share.InputValue,
                Money.Create(share.ShareMinor, expense.Currency).Value))];

            return new BalanceExpense(expense.PaidByMemberId, Money.Create(expense.AmountMinor, expense.Currency).Value, shares, expense.DeletedAt is not null);
        }

        private static BalanceSettlement BalanceSettlementOf(Settlement settlement)
        {
            return new BalanceSettlement(
                settlement.FromMemberId,
                settlement.ToMemberId,
                Money.Create(settlement.AmountMinor, settlement.Currency).Value,
                settlement.Status,
                settlement.DeletedAt is not null);
        }
    }
}
