using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Balances;
using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Guards
{
    public class FinishedOneBillTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Create_ANewBill_IsOpenAndHasNoFinishedEvent()
        {
            OneBillSetup bill = await OneBillSteps.CreateBillAsync(_app);

            await OneBillSteps.AssertStillOpenAsync(_app, bill.GroupId);
        }

        [Fact]
        public async Task GetGroup_OfAFinishedBill_ShowsFinishedToTheOwnerAndToAnAccountMember()
        {
            (OneBillSetup bill, SignedInUser bojan) = await CreateFinishedBillWithAnAccountMemberAsync();

            Assert.Equal("Finished", await ClosingSteps.StatusInGroupResponseAsync(bill.Owner, bill.GroupId));
            Assert.Equal("Finished", await ClosingSteps.StatusInGroupResponseAsync(bojan, bill.GroupId));
        }

        [Fact]
        public async Task GetList_WithAFinishedBill_PutsItUnderFinishedGroupsForTheOwnerAndTheAccountMember()
        {
            (OneBillSetup bill, SignedInUser bojan) = await CreateFinishedBillWithAnAccountMemberAsync();

            JsonElement ownerBody = await ClosingSteps.ReadGroupListAsync(bill.Owner);
            JsonElement bojanBody = await ClosingSteps.ReadGroupListAsync(bojan);

            Assert.Contains(bill.GroupId, GroupRequests.IdsOf(ownerBody.GetProperty("finishedGroups")));
            Assert.DoesNotContain(bill.GroupId, GroupRequests.IdsOf(ownerBody.GetProperty("groups")));
            Assert.Contains(bill.GroupId, GroupRequests.IdsOf(bojanBody.GetProperty("finishedGroups")));
            Assert.DoesNotContain(bill.GroupId, GroupRequests.IdsOf(bojanBody.GetProperty("groups")));
        }

        [Fact]
        public async Task AddExpense_ToAFinishedBill_Answers400GroupFinished()
        {
            (OneBillSetup bill, _) = await CreateFinishedBillWithAnAccountMemberAsync();

            HttpResponseMessage response = await ExpenseRequests.AddAsync(bill.Owner.Client, bill.GroupId, OneBillSteps.OwnerPaysEquallyInput(bill, 10_000));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_FINISHED);
        }

        [Fact]
        public async Task Unlock_OfAFinishedBill_ReopensItAndTheGetEndpointsDoNotFinishItAgain()
        {
            (OneBillSetup bill, SignedInUser bojan) = await CreateFinishedBillWithAnAccountMemberAsync();
            await ClosingSteps.UnlockAsync(bill.Owner, bill.GroupId);

            await ClosingSteps.ReadGroupAsync(bill.Owner, bill.GroupId);
            await ClosingSteps.ReadGroupListAsync(bojan);
            await BalanceRequests.ReadBalancedAsync(bill.Owner.Client, bill.GroupId);
            await SettlementSteps.ReadListAsync(bojan, bill.GroupId);

            Assert.Equal("Open", await ClosingRows.StatusAsync(_app, bill.GroupId));
            Assert.Null(await ClosingRows.FinishedAtUtcAsync(_app, bill.GroupId));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, bill.GroupId, "GroupFinished"));
        }

        [Fact]
        public async Task Restore_ThePaymentOfAnUnlockedBill_FinishesItAgainOnThatWrite()
        {
            OneBillSetup bill = await OneBillSteps.CreateBillAsync(_app);
            Guid paymentId = await OneBillSteps.MarkoPaysAsync(bill);
            await ClosingSteps.UnlockAsync(bill.Owner, bill.GroupId);
            await SettlementSteps.DeleteAsync(bill.Owner, bill.GroupId, paymentId);
            Assert.Equal("Open", await ClosingRows.StatusAsync(_app, bill.GroupId));
            Assert.Null(await ClosingRows.FinishedAtUtcAsync(_app, bill.GroupId));
            _app.Clock.Advance(ClosingSteps.OneMinute);

            await SettlementSteps.RestoreAsync(bill.Owner, bill.GroupId, paymentId);

            Assert.Equal("Finished", await ClosingRows.StatusAsync(_app, bill.GroupId));
            Assert.Equal(SettlementRows.UtcTextOf(_app.Clock.GetUtcNow()), await ClosingRows.FinishedAtUtcAsync(_app, bill.GroupId));
            Assert.Equal(2, await GroupRows.CountEventsOfTypeAsync(_app, bill.GroupId, "GroupFinished"));
        }

        [Fact]
        public async Task Record_TheFinalPaymentInANormalGroupWhereEveryoneIsThenKvit_KeepsTheGroupOpen()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await ZeroBalanceScenario.BojanOwesAnaAsync(_app, group);

            await ZeroBalanceScenario.BojanPaysAnaInFullAsync(group);

            await SettlementSteps.AssertAllBalancesZeroAsync(group.Owner, group.GroupId);
            Assert.Equal("Open", await ClosingRows.StatusAsync(_app, group.GroupId));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "GroupFinished"));
        }

        private async Task<(OneBillSetup Bill, SignedInUser Bojan)> CreateFinishedBillWithAnAccountMemberAsync()
        {
            (OneBillSetup bill, SignedInUser bojan, Guid bojanRowId) = await OneBillSteps.CreateBillWithAnAccountMemberAsync(_app);
            await OneBillSteps.MarkoPaysAsync(bill);
            await OneBillSteps.OwnerRecordsBojanPayingAsync(bill, bojanRowId, OneBillSteps.BojanShareMinor);

            return (bill, bojan);
        }
    }
}
