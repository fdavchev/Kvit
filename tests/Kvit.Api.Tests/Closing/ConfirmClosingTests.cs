using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Closing
{
    public class ConfirmClosingTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Confirm_ByAMemberWithAnAccount_Answers204WithNoBody()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            HttpResponseMessage response = await ClosingRequests.ConfirmAsync(group.Member.Client, group.GroupId);

            await SettlementSteps.AssertNoContentAsync(response);
        }

        [Fact]
        public async Task Confirm_WhenAnotherMemberIsStillWaiting_StoresAConfirmedRowWithTheClockTimeAndKeepsTheGroupClosing()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            _app.Clock.Advance(ClosingSteps.OneMinute);

            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);

            Assert.Equal(1, await ClosingResponseRows.CountAsync(_app, group.GroupId));
            Assert.Equal("Confirmed", await ClosingResponseRows.ValueAsync(_app, group.GroupId, group.MemberRowId, "response"));
            Assert.Null(await ClosingResponseRows.ValueAsync(_app, group.GroupId, group.MemberRowId, "reason"));
            Assert.Equal(
                SettlementRows.UtcTextOf(_app.Clock.GetUtcNow()),
                await ClosingResponseRows.ValueAsync(_app, group.GroupId, group.MemberRowId, "responded_at AT TIME ZONE 'UTC'"));
            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Null(await GroupRows.GroupValueAsync(_app, group.GroupId, "finished_at"));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "GroupFinished"));
        }

        [Fact]
        public async Task Confirm_ByTheLastWaitingMember_FinishesTheGroupWithTheClockTime()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);
            _app.Clock.Advance(ClosingSteps.OneMinute);

            await ClosingSteps.ConfirmAsync(group.Third, group.GroupId);

            Assert.Equal("Finished", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Equal(SettlementRows.UtcTextOf(_app.Clock.GetUtcNow()), await ClosingRows.FinishedAtUtcAsync(_app, group.GroupId));
            Assert.Equal(2, await ClosingResponseRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Confirm_InTheOtherOrder_FinishesTheGroupJustTheSame()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await ClosingSteps.ConfirmAsync(group.Third, group.GroupId);
            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, group.GroupId));

            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);

            Assert.Equal("Finished", await ClosingRows.StatusAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Confirm_PlainNamesAreNeverWaitedFor_SoTheTwoAccountMembersAreEnough()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);
            await ClosingSteps.ConfirmAsync(group.Third, group.GroupId);

            Assert.Equal("Finished", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Equal(2, await ClosingResponseRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Confirm_WhenAWaitingMemberLeftMeanwhile_TheRemainingConfirmationFinishesTheGroup()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Third.UserId, "Left");

            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);

            Assert.Equal("Finished", await ClosingRows.StatusAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Confirm_InOneGroup_DoesNotTouchAnotherGroupsClosing()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            SettlementGroup other = await ClosingSteps.CreateClosingGroupAsync(_app);

            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);

            Assert.Equal(0, await ClosingResponseRows.CountAsync(_app, other.GroupId));
            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, other.GroupId));
        }
    }
}
