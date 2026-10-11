using Kvit.Domain.Interfaces;
using Kvit.Domain.MoneyRules;

namespace Kvit.Domain.Tests.Fakes
{
    public sealed class FakeGroupMoneyRepository : IGroupMoneyRepository
    {
        private GroupMoney _money = new([], [], []);

        public List<Guid> RequestedGroupIds { get; } = [];

        public void AnswerWith(GroupMoney money)
        {
            _money = money;
        }

        public Task<GroupMoney> MoneyOfAsync(Guid groupId, CancellationToken cancellationToken)
        {
            RequestedGroupIds.Add(groupId);

            return Task.FromResult(_money);
        }
    }
}
