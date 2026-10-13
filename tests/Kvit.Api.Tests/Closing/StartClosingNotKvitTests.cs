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
    public class StartClosingNotKvitTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData(ExpenseInputs.Mkd)]
        [InlineData(ExpenseInputs.Eur)]
        public async Task Start_WhenSomeoneOwesMoney_Answers400GroupNotKvitAndWritesNothing(string currency)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group, currency);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_KVIT);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Equal("Open", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Null(await ClosingRows.ClosingStartedAtUtcAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Start_WhenOnlyPlainNamesHaveBalances_Answers400GroupNotKvit()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.AddTwoPersonExpenseAsync(_app, group, group.MarkoRowId, group.CvetaRowId);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_KVIT);
        }

        [Fact]
        public async Task Start_WhenMkdIsSettledButEurIsNot_Answers400GroupNotKvit()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group, ExpenseInputs.Mkd);
            await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group, ExpenseInputs.Mkd);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group, ExpenseInputs.Eur);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_KVIT);
        }

        [Fact]
        public async Task Start_WhenTheDebtWasOverpaid_Answers400GroupNotKvit()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId, 50_000);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_KVIT);
        }

        [Theory]
        [InlineData("Left")]
        [InlineData("Removed")]
        public async Task Start_WhenAFormerMemberStillHasABalance_Answers400GroupNotKvit(string endKind)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, endKind);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_KVIT);
        }

        [Fact]
        public async Task Start_WhenAPaymentIsPendingAndEveryBalanceIsZero_Answers400GroupNotKvitAndWritesNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, ZeroBalanceScenario.PendingStatus);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_KVIT);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
        }

        [Fact]
        public async Task Start_WhenAPendingPaymentBelongsToAPlainName_Answers400GroupNotKvit()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.PendingMarkoToBojanByTheOwnerAsync(group);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_KVIT);
        }

        [Fact]
        public async Task Start_WhenAPendingPaymentIsBetweenTwoOtherPeopleInEur_Answers400GroupNotKvit()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.RecordAsync(group.Third, group.GroupId, group.ThirdRowId, group.MemberRowId, ZeroBalanceScenario.ShareMinor, ExpenseInputs.Eur);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_KVIT);
        }

        [Fact]
        public async Task Start_WithBothANonZeroBalanceAndAPendingPayment_Answers400GroupNotKvit()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, ZeroBalanceScenario.PendingStatus);

            HttpResponseMessage response = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_KVIT);
        }

        [Fact]
        public async Task Start_PendingPaymentThatSettlesTheDebt_IsRefusedUntilTheReceiverConfirmsAndThenAnswers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);

            HttpResponseMessage refused = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);
            await SettlementSteps.ConfirmAsync(group.Owner, group.GroupId, settlementId);
            HttpResponseMessage accepted = await ClosingRequests.StartAsync(group.Owner.Client, group.GroupId);

            await ProblemResponse.AssertAsync(refused, HttpStatusCode.BadRequest, ResultCodes.GROUP_NOT_KVIT);
            await SettlementSteps.AssertNoContentAsync(accepted);
        }
    }
}
