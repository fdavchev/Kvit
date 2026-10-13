using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Balances;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class ClaimRuleSettlementTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task CanClaimNames_MemberWithNoPaymentWhileOthersHavePayments_IsTrueAndTheOthersAreFalse()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.RecordAsync(group.Third, group.GroupId, group.ThirdRowId, group.OwnerRowId);

            Assert.True(await CanClaimNamesAsync(group.Member, group.GroupId));
            Assert.False(await CanClaimNamesAsync(group.Owner, group.GroupId));
            Assert.False(await CanClaimNamesAsync(group.Third, group.GroupId));
        }

        [Theory]
        [InlineData(ZeroBalanceScenario.PendingStatus)]
        [InlineData(ZeroBalanceScenario.ConfirmedStatus)]
        [InlineData(ZeroBalanceScenario.RejectedStatus)]
        [InlineData(ZeroBalanceScenario.CancelledStatus)]
        public async Task CanClaimNames_MemberWhoIsThePayerOfAPaymentInAnyStatus_IsFalse(string status)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, status);

            Assert.False(await CanClaimNamesAsync(group.Member, group.GroupId));
        }

        [Theory]
        [InlineData(ZeroBalanceScenario.PendingStatus)]
        [InlineData(ZeroBalanceScenario.ConfirmedStatus)]
        [InlineData(ZeroBalanceScenario.RejectedStatus)]
        [InlineData(ZeroBalanceScenario.CancelledStatus)]
        public async Task CanClaimNames_MemberWhoIsTheReceiverOfAPaymentInAnyStatus_IsFalse(string status)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.AnaPaysBojanWithStatusAsync(group, status);

            Assert.False(await CanClaimNamesAsync(group.Member, group.GroupId));
        }

        [Fact]
        public async Task CanClaimNames_MemberWhoseOnlyPaymentWasDeleted_IsStillFalse()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group);
            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);

            Assert.False(await CanClaimNamesAsync(group.Member, group.GroupId));
        }

        [Fact]
        public async Task CanClaimNames_MemberWhoseOnlyPendingPaymentRowIsDeleted_IsStillFalse()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementTableRows.InsertSettlementAsync(_app, group.GroupId, group.MemberRowId, group.OwnerRowId, group.Member.UserId, deletedAt: DateTimeOffset.UtcNow);

            Assert.False(await CanClaimNamesAsync(group.Member, group.GroupId));
        }

        [Theory]
        [InlineData(ZeroBalanceScenario.PendingStatus)]
        [InlineData(ZeroBalanceScenario.ConfirmedStatus)]
        [InlineData(ZeroBalanceScenario.RejectedStatus)]
        [InlineData(ZeroBalanceScenario.CancelledStatus)]
        public async Task Claim_MemberWhoIsThePayerOfAPaymentInAnyStatus_Answers400MemberCannotClaimAndWritesNothing(string status)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, status);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.ClaimAsync(group.Member.Client, group.GroupId, group.MarkoRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_CANNOT_CLAIM);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "end_kind"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, group.MarkoRowId, "user_id"));
        }

        [Theory]
        [InlineData(ZeroBalanceScenario.PendingStatus)]
        [InlineData(ZeroBalanceScenario.ConfirmedStatus)]
        [InlineData(ZeroBalanceScenario.RejectedStatus)]
        [InlineData(ZeroBalanceScenario.CancelledStatus)]
        public async Task Claim_MemberWhoIsTheReceiverOfAPaymentInAnyStatus_Answers400MemberCannotClaimAndWritesNothing(string status)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.AnaPaysBojanWithStatusAsync(group, status);
            string before = await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.ClaimAsync(group.Member.Client, group.GroupId, group.MarkoRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_CANNOT_CLAIM);
            Assert.Equal(before, await ZeroBalanceScenario.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Claim_MemberWhoseOnlyPaymentWasDeleted_Answers400MemberCannotClaim()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group);
            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await GroupRequests.ClaimAsync(group.Member.Client, group.GroupId, group.MarkoRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_CANNOT_CLAIM);
        }

        [Fact]
        public async Task Claim_MemberWhoseOnlyPaymentWasDeletedThenRestored_Answers400MemberCannotClaim()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group);
            await SettlementSteps.DeleteAsync(group.Owner, group.GroupId, settlementId);
            await SettlementSteps.RestoreAsync(group.Owner, group.GroupId, settlementId);

            HttpResponseMessage response = await GroupRequests.ClaimAsync(group.Member.Client, group.GroupId, group.MarkoRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_CANNOT_CLAIM);
        }

        [Fact]
        public async Task Claim_MemberWithAPaymentAndAnUnknownTarget_Answers404MemberNotFoundBeforeTheOwnRowCheck()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanPaysAnaWithStatusAsync(group, ZeroBalanceScenario.PendingStatus);

            HttpResponseMessage response = await GroupRequests.ClaimAsync(group.Member.Client, group.GroupId, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public async Task Claim_MemberWhileOnlyOthersHavePayments_Answers204()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.RecordAsync(group.Third, group.GroupId, group.ThirdRowId, group.OwnerRowId);

            HttpResponseMessage response = await GroupRequests.ClaimAsync(group.Member.Client, group.GroupId, group.MarkoRowId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("SetAside", await GroupRows.MemberRowValueAsync(_app, group.MemberRowId, "end_kind"));
        }

        [Fact]
        public async Task Claim_PlainNameThatOnlyHasPaymentsRecorded_Answers204AndTheClaimerCannotClaimAgain()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.ThirdRowId, group.MarkoRowId);

            HttpResponseMessage response = await GroupRequests.ClaimAsync(group.Member.Client, group.GroupId, group.MarkoRowId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(group.Member.UserId.ToString(), await GroupRows.MemberRowValueAsync(_app, group.MarkoRowId, "user_id"));
            Assert.False(await CanClaimNamesAsync(group.Member, group.GroupId));
        }

        [Fact]
        public async Task Claim_PlainNameThatOnlyHasPaymentsRecorded_KeepsThePaymentOnTheNameRowNowShownAsTheClaimer()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.ThirdRowId, group.MarkoRowId);

            await GroupRequests.ClaimAsync(group.Member.Client, group.GroupId, group.MarkoRowId);

            JsonElement row = SettlementJson.RowWithId(await SettlementSteps.ReadListAsync(group.Member, group.GroupId), settlementId);
            Assert.Equal(group.MarkoRowId, row.GetProperty("toMemberId").GetGuid());
            Assert.Equal(group.ThirdRowId, row.GetProperty("fromMemberId").GetGuid());
            Assert.Equal("Bojan", row.GetProperty("toName").GetString());
            Assert.Equal("Confirmed", row.GetProperty("status").GetString());
            Assert.Equal(ZeroBalanceScenario.ShareMinor, row.GetProperty("amountMinor").GetInt64());
        }

        [Fact]
        public async Task Claim_PlainNameThatOnlyHasPaymentsRecorded_KeepsThePaymentInTheBalancesOfTheClaimersNameRow()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.ThirdRowId, group.MarkoRowId);

            await GroupRequests.ClaimAsync(group.Member.Client, group.GroupId, group.MarkoRowId);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            JsonElement mkd = BalanceJson.CurrencyOf(body, ExpenseInputs.Mkd);
            Assert.Equal([group.OwnerRowId, group.MarkoRowId, group.CvetaRowId, group.ThirdRowId], BalanceJson.BalanceMemberIds(mkd));
            Assert.Equal(["Ana", "Bojan", "Cveta", "Dime"], BalanceJson.BalanceNames(mkd));
            Assert.Equal([0L, -30_000L, 0L, 30_000L], BalanceJson.BalanceMinors(mkd));
        }

        [Fact]
        public async Task Claim_PlainNameWithAPendingPayment_LetsTheClaimerCancelItAsThePayer()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MarkoRowId, group.ThirdRowId);
            await GroupRequests.ClaimAsync(group.Member.Client, group.GroupId, group.MarkoRowId);

            await SettlementSteps.CancelAsync(group.Member, group.GroupId, settlementId);

            Assert.Equal("Cancelled", await SettlementRows.ValueAsync(_app, settlementId, "status"));
        }

        [Fact]
        public async Task Claim_PlainNameWithPaymentsAndExpenses_KeepsEveryBalanceSummingToZero()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.MarkoOwesAnaAsync(_app, group);
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MarkoRowId, group.OwnerRowId, 10_000);

            HttpResponseMessage response = await GroupRequests.ClaimAsync(group.Member.Client, group.GroupId, group.MarkoRowId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            Assert.Equal([20_000L, -20_000L, 0L, 0L], BalanceJson.BalanceMinors(BalanceJson.CurrencyOf(body, ExpenseInputs.Mkd)));
        }

        private async Task<bool> CanClaimNamesAsync(SignedInUser viewer, Guid groupId)
        {
            JsonElement body = await GroupRequests.ReadMembersAsync(viewer.Client, groupId);

            return body.GetProperty("canClaimNames").GetBoolean();
        }
    }
}
