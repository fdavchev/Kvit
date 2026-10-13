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
    public class ConfirmClosingRefusedTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Confirm_ByTheOwner_Answers400ClosingOwnerCannotAnswerAndChangesNothing()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ClosingRequests.ConfirmAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.CLOSING_OWNER_CANNOT_ANSWER);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(0, await ClosingResponseRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Confirm_ByTheOwnerWhileTheGroupIsOpen_Answers400ClosingOwnerCannotAnswerBeforeTheStateIsChecked()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            HttpResponseMessage response = await ClosingRequests.ConfirmAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.CLOSING_OWNER_CANNOT_ANSWER);
        }

        [Fact]
        public async Task Confirm_ByAStrangerWhileTheGroupIsClosing_Answers404GroupNotFoundAndStoresNothing()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ClosingRequests.ConfirmAsync(stranger.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(0, await ClosingResponseRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Confirm_ByAStrangerWhileTheGroupIsOpen_Answers404GroupNotFoundBeforeTheStateIsChecked()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");

            HttpResponseMessage response = await ClosingRequests.ConfirmAsync(stranger.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        [Theory]
        [InlineData("Left")]
        [InlineData("Removed")]
        public async Task Confirm_ByAMemberWhoIsNoLongerInTheGroup_Answers404GroupNotFoundAndStoresNothing(string endKind)
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, endKind);

            HttpResponseMessage response = await ClosingRequests.ConfirmAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(0, await ClosingResponseRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Confirm_WhenTheGroupIsOpen_Answers400GroupNotClosingAndStoresNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ClosingRequests.ConfirmAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_CLOSING);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(0, await ClosingResponseRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Confirm_AfterTheLastConfirmationFinishedTheGroup_Answers400GroupNotClosing()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ClosingRequests.ConfirmAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_CLOSING);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(2, await ClosingResponseRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Confirm_Twice_SecondAnswers400ClosingAlreadyAnsweredAndKeepsOneRowAndOneEvent()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await ClosingSteps.ConfirmAsync(group.Member, group.GroupId);
            string respondedAt = (await ClosingResponseRows.ValueAsync(_app, group.GroupId, group.MemberRowId, "responded_at"))!;
            _app.Clock.Advance(ClosingSteps.OneMinute);

            HttpResponseMessage response = await ClosingRequests.ConfirmAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.CLOSING_ALREADY_ANSWERED);
            Assert.Equal(1, await ClosingResponseRows.CountAsync(_app, group.GroupId));
            Assert.Equal(respondedAt, await ClosingResponseRows.ValueAsync(_app, group.GroupId, group.MemberRowId, "responded_at"));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ClosingConfirmed"));
            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, group.GroupId));
        }
    }
}
