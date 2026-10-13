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
    public class ObjectToClosingRefusedTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Object_ReasonOf201Characters_Answers400ClosingReasonTooLongAndChangesNothing()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await ClosingSteps.ConfirmAsync(group.Third, group.GroupId);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ClosingRequests.ObjectAsync(group.Member.Client, group.GroupId, ExpenseInputs.TextOfLength(201));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.CLOSING_REASON_TOO_LONG);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Equal(1, await ClosingResponseRows.CountAsync(_app, group.GroupId));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ClosingObjected"));
        }

        [Fact]
        public async Task Object_ReasonOf201CyrillicLetters_Answers400ClosingReasonTooLong()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);

            HttpResponseMessage response = await ClosingRequests.ObjectAsync(group.Member.Client, group.GroupId, new string('а', 201));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.CLOSING_REASON_TOO_LONG);
        }

        [Fact]
        public async Task Object_ByTheOwner_Answers400ClosingOwnerCannotAnswerAndChangesNothing()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ClosingRequests.ObjectAsync(group.Owner.Client, group.GroupId, "I changed my mind");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.CLOSING_OWNER_CANNOT_ANSWER);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Object_ByAStrangerWhileTheGroupIsClosing_Answers404GroupNotFoundAndChangesNothing()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ClosingRequests.ObjectAsync(stranger.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
        }

        [Theory]
        [InlineData("Left")]
        [InlineData("Removed")]
        public async Task Object_ByAMemberWhoIsNoLongerInTheGroup_Answers404GroupNotFoundAndKeepsTheGroupClosing(string endKind)
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, endKind);

            HttpResponseMessage response = await ClosingRequests.ObjectAsync(group.Member.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal("Closing", await ClosingRows.StatusAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Object_WhenTheGroupIsOpen_Answers400GroupNotClosingAndWritesNoEvent()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ClosingRequests.ObjectAsync(group.Member.Client, group.GroupId, "No");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_CLOSING);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Object_WhenTheGroupIsFinished_Answers400GroupNotClosingAndKeepsItFinished()
        {
            SettlementGroup group = await ClosingSteps.CreateFinishedGroupAsync(_app);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ClosingRequests.ObjectAsync(group.Member.Client, group.GroupId, "Too late");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_CLOSING);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(2, await ClosingResponseRows.CountAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Object_Twice_SecondAnswers400GroupNotClosingAndWritesOneObjectedEvent()
        {
            SettlementGroup group = await ClosingSteps.CreateClosingGroupAsync(_app);
            await ClosingSteps.ObjectAsync(group.Member, group.GroupId, "First");

            HttpResponseMessage response = await ClosingRequests.ObjectAsync(group.Third.Client, group.GroupId, "Second");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_CLOSING);
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ClosingObjected"));
        }
    }
}
