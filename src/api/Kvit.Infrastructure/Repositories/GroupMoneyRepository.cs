using Kvit.Domain.Interfaces;
using Kvit.Domain.MoneyRules;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Infrastructure.Repositories
{
    public sealed class GroupMoneyRepository(AppDbContext _context) : IGroupMoneyRepository
    {
        public async Task<GroupMoney> MoneyOfAsync(Guid groupId, CancellationToken cancellationToken)
        {
            List<Guid> memberIds = await _context.NamedGroupMembers(groupId)
                .AsNoTracking()
                .InJoiningOrder()
                .Select(named => named.Member.Id)
                .ToListAsync(cancellationToken);
            List<BalanceExpense> expenses = await _context.CountedBalanceExpensesOfAsync(groupId, cancellationToken);
            List<BalanceSettlement> settlements = await _context.CountedBalanceSettlementsOfAsync(groupId, cancellationToken);

            return new GroupMoney(memberIds, expenses, settlements);
        }
    }
}
