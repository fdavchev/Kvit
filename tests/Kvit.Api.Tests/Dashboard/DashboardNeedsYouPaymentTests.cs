using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Dashboard
{
    public class DashboardNeedsYouPaymentTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task NeedsYou_PendingPaymentToTheCaller_AnswersAPaymentItemWithExactlyTheSevenFields()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.PendingBojanToDimeAsync(group);

            JsonElement body = await DashboardRequests.ReadAsync(group.Third);

            JsonElement item = Assert.Single(DashboardJson.PaymentItemsOf(body));
            Assert.Equal(DashboardJson.PaymentItemFieldNames, GroupRequests.PropertyNamesOf(item));
        }

        [Fact]
        public async Task NeedsYou_PendingPaymentToTheCaller_AnswersTheGroupThePayerTheAmountAndTheCurrency()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToDimeAsync(group);

            JsonElement body = await DashboardRequests.ReadAsync(group.Third);

            JsonElement item = Assert.Single(DashboardJson.PaymentItemsOf(body));
            Assert.Equal("payment", item.GetProperty("type").GetString());
            Assert.Equal(group.GroupId, item.GetProperty("groupId").GetGuid());
            Assert.Equal(GroupRows.DefaultName, item.GetProperty("groupName").GetString());
            Assert.Equal(settlementId, item.GetProperty("settlementId").GetGuid());
            Assert.Equal(GroupsApp.MemberName, item.GetProperty("fromName").GetString());
            Assert.Equal(SettlementRequests.DefaultAmountMinor, item.GetProperty("amountMinor").GetInt64());
            Assert.Equal("MKD", item.GetProperty("currency").GetString());
        }

        [Fact]
        public async Task NeedsYou_PendingEurPayment_AnswersTheEurCurrencyAndAmount()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.ThirdRowId, 1_250, ExpenseInputs.Eur);

            JsonElement body = await DashboardRequests.ReadAsync(group.Third);

            JsonElement item = Assert.Single(DashboardJson.PaymentItemsOf(body));
            Assert.Equal("EUR", item.GetProperty("currency").GetString());
            Assert.Equal(1_250, item.GetProperty("amountMinor").GetInt64());
        }

        [Fact]
        public async Task NeedsYou_PendingPaymentToAnotherAccountMember_ShowsNothingToThePayerOrTheOwner()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.PendingBojanToDimeAsync(group);

            JsonElement payerBody = await DashboardRequests.ReadAsync(group.Member);
            JsonElement ownerBody = await DashboardRequests.ReadAsync(group.Owner);

            Assert.Empty(DashboardJson.PaymentItemsOf(payerBody));
            Assert.Empty(DashboardJson.PaymentItemsOf(ownerBody));
        }

        [Fact]
        public async Task NeedsYou_PendingPaymentToAPlainNameMember_ShowsToTheOwnerOnly()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToMarkoAsync(group);

            JsonElement ownerBody = await DashboardRequests.ReadAsync(group.Owner);
            JsonElement payerBody = await DashboardRequests.ReadAsync(group.Member);
            JsonElement thirdBody = await DashboardRequests.ReadAsync(group.Third);

            JsonElement item = Assert.Single(DashboardJson.PaymentItemsOf(ownerBody));
            Assert.Equal(settlementId, item.GetProperty("settlementId").GetGuid());
            Assert.Equal(GroupsApp.MemberName, item.GetProperty("fromName").GetString());
            Assert.Empty(DashboardJson.PaymentItemsOf(payerBody));
            Assert.Empty(DashboardJson.PaymentItemsOf(thirdBody));
        }

        [Fact]
        public async Task NeedsYou_PendingPaymentToTheOwner_ShowsToTheOwnerAsTheReceiver()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);

            JsonElement body = await DashboardRequests.ReadAsync(group.Owner);

            Assert.Equal([settlementId], DashboardJson.SettlementIdsOf(body));
        }

        [Fact]
        public async Task NeedsYou_PaymentTheOwnerMadeToAPlainName_IsConfirmedAtOnceAndShowsNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.ConfirmedAnaToMarkoAsync(group);

            JsonElement body = await DashboardRequests.ReadAsync(group.Owner);

            Assert.Empty(DashboardJson.PaymentItemsOf(body));
        }

        [Fact]
        public async Task NeedsYou_PaymentTheCallerPaidHerself_IsNotAToDo()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.PendingAnaToBojanAsync(group);

            JsonElement body = await DashboardRequests.ReadAsync(group.Owner);

            Assert.Empty(DashboardJson.PaymentItemsOf(body));
        }

        [Fact]
        public async Task NeedsYou_PaymentThatWasConfirmed_IsGone()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToDimeAsync(group);
            await SettlementSteps.ConfirmAsync(group.Third, group.GroupId, settlementId);

            JsonElement body = await DashboardRequests.ReadAsync(group.Third);

            Assert.Empty(DashboardJson.PaymentItemsOf(body));
        }

        [Fact]
        public async Task NeedsYou_PaymentThatWasRejected_IsGone()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToDimeAsync(group);
            await SettlementSteps.RejectAsync(group.Third, group.GroupId, settlementId);

            JsonElement body = await DashboardRequests.ReadAsync(group.Third);

            Assert.Empty(DashboardJson.PaymentItemsOf(body));
        }

        [Fact]
        public async Task NeedsYou_PaymentThatWasCancelledByThePayer_IsGone()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToDimeAsync(group);
            await SettlementSteps.CancelAsync(group.Member, group.GroupId, settlementId);

            JsonElement body = await DashboardRequests.ReadAsync(group.Third);

            Assert.Empty(DashboardJson.PaymentItemsOf(body));
        }

        [Fact]
        public async Task NeedsYou_PaymentThatWasConfirmedByTheOwnerForAPlainName_IsGoneForTheOwner()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToMarkoAsync(group);
            await SettlementSteps.ConfirmAsync(group.Owner, group.GroupId, settlementId);

            JsonElement body = await DashboardRequests.ReadAsync(group.Owner);

            Assert.Empty(DashboardJson.PaymentItemsOf(body));
        }

        [Fact]
        public async Task NeedsYou_TwoPendingPaymentsToTheCaller_ListsBoth()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid fromBojan = await SettlementSteps.PendingBojanToDimeAsync(group);
            Guid fromAna = await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.OwnerRowId, group.ThirdRowId);

            JsonElement body = await DashboardRequests.ReadAsync(group.Third);

            Assert.Equal([.. new[] { fromBojan, fromAna }.Order()], DashboardJson.SettlementIdsOf(body));
        }

        [Fact]
        public async Task NeedsYou_PendingPaymentsInTwoGroups_ListsBothWithTheirOwnGroupName()
        {
            SignedInUser ana = await _app.SignUpAsync();
            ExpenseGroup flat = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "Flat");
            ExpenseGroup trip = await DashboardSteps.CreateGroupWithNewMemberAsync(_app, ana, "Trip");
            Guid flatPayment = await SettlementSteps.RecordAsync(flat.Member, flat.GroupId, flat.MemberRowId, flat.OwnerRowId);
            Guid tripPayment = await SettlementSteps.RecordAsync(trip.Member, trip.GroupId, trip.MemberRowId, trip.OwnerRowId);

            JsonElement body = await DashboardRequests.ReadAsync(ana);

            List<JsonElement> items = DashboardJson.PaymentItemsOf(body);
            Assert.Equal(2, items.Count);
            Assert.Equal("Flat", items.Single(item => item.GetProperty("settlementId").GetGuid() == flatPayment).GetProperty("groupName").GetString());
            Assert.Equal("Trip", items.Single(item => item.GetProperty("settlementId").GetGuid() == tripPayment).GetProperty("groupName").GetString());
        }

        [Fact]
        public async Task NeedsYou_PendingPaymentInADeletedGroup_ShowsNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.PendingBojanToDimeAsync(group);
            await GroupRows.SetDeletedAsync(_app, group.GroupId, group.Owner.UserId, 1);

            JsonElement body = await DashboardRequests.ReadAsync(group.Third);

            Assert.Empty(DashboardJson.PaymentItemsOf(body));
        }

        [Fact]
        public async Task NeedsYou_PendingPaymentInAGroupTheCallerLeft_ShowsNothing()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.PendingBojanToDimeAsync(group);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Third.UserId, "Left");

            JsonElement body = await DashboardRequests.ReadAsync(group.Third);

            Assert.Empty(DashboardJson.PaymentItemsOf(body));
        }

        [Fact]
        public async Task NeedsYou_PendingPaymentsOfOtherPeoplesGroups_NeverShowUp()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.PendingBojanToDimeAsync(group);
            SignedInUser stranger = await _app.SignUpAsync();

            JsonElement body = await DashboardRequests.ReadAsync(stranger);

            Assert.Equal(0, body.GetProperty("needsYou").GetArrayLength());
        }
    }
}
