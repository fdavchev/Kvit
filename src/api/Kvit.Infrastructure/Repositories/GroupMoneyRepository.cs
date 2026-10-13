using Kvit.Domain.Interfaces;
using Kvit.Domain.MoneyRules;
using Kvit.Infrastructure.Persistence;

namespace Kvit.Infrastructure.Repositories
{
    public sealed class GroupMoneyRepository(AppDbContext _context) : IGroupMoneyRepository
    {
        public Task<GroupMoney> MoneyOfAsync(Guid groupId, CancellationToken cancellationToken)
        {
            return _context.GroupMoneyOfAsync(groupId, cancellationToken);
        }
    }
}
