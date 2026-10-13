using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Closing
{
    public class StartClosingRefusedTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Start_ByAMemberWhoIsNotTheOwner_Answers403GroupNotOwnerAndChangesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Equal("Open", await ClosingRows.StatusAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Start_ByAMemberWhoIsNotTheOwnerWhileSomeoneOwesMoney_Answers403GroupNotOwnerBeforeTheMoneyIsChecked()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Third.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
        }

        [Fact]
        public async Task Start_ByAMemberWhoIsNotTheOwnerWhileTheGroupIsClosing_Answers403GroupNotOwnerBeforeTheStateIsChecked()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
        }

        [Fact]
        public async Task Start_WhenTheGroupIsAlreadyClosing_Answers400GroupNotOpenAndKeepsTheFirstStartTimeAndOneEvent()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            string startedAt = (await ClosingRows.ClosingStartedAtUtcAsync(_app, group.GroupId))!;
            _app.Clock.Advance(ClosingSteps.OneMinute);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_OPEN);
            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Equal(startedAt, await ClosingRows.ClosingStartedAtUtcAsync(_app, group.GroupId));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ClosingStarted"));
        }

        [Fact]
        public async Task Start_WhenTheGroupIsFinished_Answers400GroupNotOpenAndChangesNothing()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_OPEN);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Start_OnAOneBill_Answers400GroupIsOneBillAndTheGroupStaysOpen()
        {
            SignedInUser owner = await _app.SignUpAsync();
            Guid groupId = await ClosingSteps.CreateEmptyOneBillAsync(_app, owner);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, groupId);

            HttpResponseMessage response = await ClosingRequests.StartAsync(owner.Client, groupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_IS_ONE_BILL);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, groupId));
            Assert.Equal("Open", await ClosingRows.StatusAsync(_app, groupId));
        }

        [Fact]
        public async Task Start_OnAOneBillWhereSomeoneOwesMoney_Answers400GroupIsOneBillNotGroupNotKvit()
        {
            SignedInUser owner = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(30_000, ExpenseInputs.Mkd, "Marko");
            (Guid groupId, _) = await _app.CreateOneBillAsync(owner, input);

            HttpResponseMessage response = await ClosingRequests.StartAsync(owner.Client, groupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_IS_ONE_BILL);
        }

        [Fact]
        public async Task Start_OnAOneBillByAMemberWhoIsNotTheOwner_Answers403GroupNotOwnerBeforeTheKindIsChecked()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser member = await _app.SignUpAsync();
            Guid groupId = await ClosingSteps.CreateEmptyOneBillAsync(_app, owner);
            await GroupRows.InsertMemberAsync(_app, groupId, member.UserId, member.Form.DisplayName, owner.UserId);

            HttpResponseMessage response = await ClosingRequests.StartAsync(member.Client, groupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
        }
    }
}
