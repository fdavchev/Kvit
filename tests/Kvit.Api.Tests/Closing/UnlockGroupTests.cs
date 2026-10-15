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
    public class UnlockGroupTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Unlock_FinishedGroupByTheOwner_Answers204WithNoBody()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);

            HttpResponseMessage response = await ClosingRequests.UnlockAsync(group.Owner.Client, group.GroupId);

            await SettlementSteps.AssertNoContentAsync(response);
        }

        [Fact]
        public async Task Unlock_FinishedGroup_ReturnsItToOpenAndClearsBothTimes()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);

            await ClosingSteps.UnlockAsync(group.Owner, group.GroupId);

            Assert.Equal("Open", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Null(await GroupRows.GroupValueAsync(_app, group.GroupId, "finished_at"));
            Assert.Null(await GroupRows.GroupValueAsync(_app, group.GroupId, "closing_started_at"));
        }

        [Fact]
        public async Task Unlock_GroupThatFinishedAtOnce_ReturnsItToOpen()
        {
            LoneOwnerGroup group = await ClosingSteps.CreateGroupWithPlainNamesOnlyAsync(_app, "Marko");
            await ClosingSteps.StartAsync(group.Owner, group.GroupId);

            await ClosingSteps.UnlockAsync(group.Owner, group.GroupId);

            Assert.Equal("Open", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Null(await GroupRows.GroupValueAsync(_app, group.GroupId, "finished_at"));
        }

        [Fact]
        public async Task Unlock_ThenStartAgain_EveryMemberHasToConfirmAgain()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);
            await ClosingSteps.UnlockAsync(group.Owner, group.GroupId);
            _app.Clock.Advance(ClosingSteps.OneMinute);
            await ClosingSteps.StartAsync(group.Owner, group.GroupId);

            HttpResponseMessage response = await ClosingRequests.ConfirmAsync(group.Member.Client, group.GroupId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Equal(1, await ClosingResponseRows.CountAsync(_app, group.GroupId));
            Assert.Equal(SettlementRows.UtcTextOf(_app.Clock.GetUtcNow()), await ClosingRows.ClosingStartedAtUtcAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Unlock_ByAMemberWhoIsNotTheOwner_Answers403GroupNotOwnerAndKeepsTheGroupFinished()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ClosingRequests.UnlockAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Equal("Finished", await ClosingRows.StatusAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Unlock_ByAMemberWhoIsNotTheOwnerWhileTheGroupIsOpen_Answers403GroupNotOwnerBeforeTheStateIsChecked()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            HttpResponseMessage response = await ClosingRequests.UnlockAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
        }

        [Fact]
        public async Task Unlock_ByAStranger_Answers404GroupNotFoundAndKeepsTheGroupFinished()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");

            HttpResponseMessage response = await ClosingRequests.UnlockAsync(stranger.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal("Finished", await ClosingRows.StatusAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Unlock_WhenTheGroupIsOpen_Answers400GroupNotFinishedAndWritesNoEvent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ClosingRequests.UnlockAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_FINISHED);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Unlock_WhenTheGroupIsStillClosing_Answers400GroupNotFinishedAndKeepsItClosing()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ClosingRequests.UnlockAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_FINISHED);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(1, await ClosingResponseRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Unlock_Twice_SecondAnswers400GroupNotFinishedAndWritesOneReopenedEvent()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);
            await ClosingSteps.UnlockAsync(group.Owner, group.GroupId);

            HttpResponseMessage response = await ClosingRequests.UnlockAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_FINISHED);
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "GroupReopened"));
        }
    }
}
