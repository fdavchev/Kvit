using Kvit.Domain.MoneyRules;

namespace Kvit.Domain.Interfaces
{
    public interface IGroupMoneyRepository
    {
        Task<GroupMoney> MoneyOfAsync(Guid groupId, CancellationToken cancellationToken);
    }
}
