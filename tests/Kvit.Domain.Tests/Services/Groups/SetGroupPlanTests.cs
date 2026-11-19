using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Domain.Services.Groups;
using Kvit.Domain.Tests.Entities;
using Kvit.Domain.Tests.Fakes;
using Xunit;

namespace Kvit.Domain.Tests.Services.Groups
{
    public class SetGroupPlanTests
    {
        private const long TwoThousandMkd = 200_000;

        private readonly FakeGroupRepository _groups = new();
        private readonly FakeActivityEventRepository _activityEvents = new();
        private readonly SetGroupPlan _setGroupPlan;
        private readonly Group _group = GroupTestData.NewGroup();

        public SetGroupPlanTests()
        {
            _groups.Seed(_group, GroupTestData.OwnerId, GroupTestData.MemberId);
            _setGroupPlan = new SetGroupPlan(_groups, _activityEvents, new FixedTimeProvider(GroupTestData.Moment));
        }

        [Fact]
        public async Task Execute_ANewPlan_MarksThePlanOfTheGroupChangedSoBothColumnsAreSaved()
        {
            Result result = await ExecuteAsync(TwoThousandMkd, "MKD");

            Assert.True(result.IsSuccess);
            Assert.Equal(TwoThousandMkd, Assert.Single(_groups.MarkedPlanChanged).PlanAmountMinor);
        }

        [Fact]
        public async Task Execute_OnlyTheAmountChanges_StillMarksThePlanChanged()
        {
            await ExecuteAsync(TwoThousandMkd, "MKD");

            await ExecuteAsync(TwoThousandMkd + 100, "MKD");

            Assert.Equal(2, _groups.MarkedPlanChanged.Count);
        }

        [Fact]
        public async Task Execute_TheSamePlanAgain_MarksNothingAndWritesNoSecondEvent()
        {
            await ExecuteAsync(TwoThousandMkd, "MKD");

            Result result = await ExecuteAsync(TwoThousandMkd, "MKD");

            Assert.True(result.IsSuccess);
            Assert.Single(_groups.MarkedPlanChanged);
            Assert.Single(_activityEvents.Added);
        }

        private Task<Result> ExecuteAsync(long amountMinor, string currency)
        {
            return _setGroupPlan.Execute(_group.Id, GroupTestData.OwnerId, amountMinor, currency, TestContext.Current.CancellationToken);
        }
    }
}
