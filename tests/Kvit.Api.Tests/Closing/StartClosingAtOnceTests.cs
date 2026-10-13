using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Closing
{
    public class StartClosingAtOnceTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Start_OwnerAlone_Answers204AndFinishesTheGroupAtOnceWithTheClockTime()
        {
            LoneOwnerGroup group = await ClosingSteps.CreateGroupWithPlainNamesOnlyAsync(_app);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Equal("Finished", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Equal(SettlementRows.UtcTextOf(_app.Clock.GetUtcNow()), await ClosingRows.FinishedAtUtcAsync(_app, group.GroupId));
            Assert.Equal(0, await ClosingResponseRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Start_OwnerWithOnlyPlainNames_FinishesTheGroupAtOnce()
        {
            LoneOwnerGroup group = await ClosingSteps.CreateGroupWithPlainNamesOnlyAsync(_app, "Marko", "Cveta", "Grandma");

            await ClosingSteps.StartAsync(group.Owner, group.GroupId);

            Assert.Equal("Finished", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.NotNull(await ClosingRows.FinishedAtUtcAsync(_app, group.GroupId));
        }

        [Theory]
        [InlineData("Left")]
        [InlineData("Removed")]
        public async Task Start_OwnerWhoseOnlyOtherAccountMemberIsGone_FinishesTheGroupAtOnce(string endKind)
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, endKind);

            await ClosingSteps.StartAsync(setup.Owner, setup.GroupId);

            Assert.Equal("Finished", await ClosingRows.StatusAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task Start_OwnerAlone_WritesOneClosingStartedAndOneGroupFinishedEventBothByTheOwner()
        {
            LoneOwnerGroup group = await ClosingSteps.CreateGroupWithPlainNamesOnlyAsync(_app, "Marko");

            await ClosingSteps.StartAsync(group.Owner, group.GroupId);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ClosingStarted"));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "GroupFinished"));
            Assert.Equal(group.Owner.UserId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "ClosingStarted", "actor_user_id"));
            Assert.Equal(group.Owner.UserId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "GroupFinished", "actor_user_id"));
        }

        [Fact]
        public async Task Start_OwnerAlone_ShowsFinishedWhenTheGroupIsRead()
        {
            LoneOwnerGroup group = await ClosingSteps.CreateGroupWithPlainNamesOnlyAsync(_app, "Marko");

            await ClosingSteps.StartAsync(group.Owner, group.GroupId);

            Assert.Equal("Finished", await ClosingSteps.StatusInGroupResponseAsync(group.Owner, group.GroupId));
        }

        [Fact]
        public async Task Start_OwnerAlone_PutsTheGroupInTheFinishedPartOfTheList()
        {
            LoneOwnerGroup group = await ClosingSteps.CreateGroupWithPlainNamesOnlyAsync(_app, "Marko");

            await ClosingSteps.StartAsync(group.Owner, group.GroupId);

            JsonElement body = await ClosingSteps.ReadGroupListAsync(group.Owner);
            Assert.Equal([group.GroupId], GroupRequests.IdsOf(body.GetProperty("finishedGroups")));
            Assert.Empty(GroupRequests.IdsOf(body.GetProperty("groups")));
        }

        [Fact]
        public async Task Start_OwnerAlone_CanBeUnlockedAndThenStartedAgain()
        {
            LoneOwnerGroup group = await ClosingSteps.CreateGroupWithPlainNamesOnlyAsync(_app, "Marko");
            await ClosingSteps.StartAsync(group.Owner, group.GroupId);
            await ClosingSteps.UnlockAsync(group.Owner, group.GroupId);
            _app.Clock.Advance(ClosingSteps.OneMinute);

            await ClosingSteps.StartAsync(group.Owner, group.GroupId);

            Assert.Equal("Finished", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Equal(SettlementRows.UtcTextOf(_app.Clock.GetUtcNow()), await ClosingRows.FinishedAtUtcAsync(_app, group.GroupId));
        }
    }
}
