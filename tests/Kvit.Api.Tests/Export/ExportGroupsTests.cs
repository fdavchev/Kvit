using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Export
{
    public class ExportGroupsTests(ExportApp _app) : IClassFixture<ExportApp>
    {
        [Fact]
        public async Task Export_Groups_AreExactlyTheCurrentGroupAndTheLeftGroup()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            Assert.Equal(
                new[] { scenario.GroupId, scenario.LeftGroupId }.Order(),
                ExportBody.IdsOf(body.Groups).Order());
        }

        [Fact]
        public async Task Export_GroupTheExporterNeverJoined_IsNotListed()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            Assert.DoesNotContain(scenario.StrangersGroupId, ExportBody.IdsOf(body.Groups));
            body.AssertDoesNotMention(ExportScenarioSteps.StrangersGroupName);
        }

        [Fact]
        public async Task Export_GroupTheExporterOwns_ShowsNameEmojiCurrencyAndStatusAsStored()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement group = body.GroupWithId(scenario.GroupId);

            Assert.Equal(await GroupRows.GroupValueAsync(_app, scenario.GroupId, "name"), group.GetProperty("name").GetString());
            Assert.Equal(await GroupRows.GroupValueAsync(_app, scenario.GroupId, "emoji"), group.GetProperty("emoji").GetString());
            Assert.Equal(await GroupRows.GroupValueAsync(_app, scenario.GroupId, "default_currency"), group.GetProperty("defaultCurrency").GetString());
            Assert.Equal("Open", group.GetProperty("status").GetString());
        }

        [Fact]
        public async Task Export_GroupTheExporterOwns_ShowsIsOwnerTrue()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            Assert.True(body.GroupWithId(scenario.GroupId).GetProperty("isOwner").GetBoolean());
        }

        [Fact]
        public async Task Export_GroupWhereTheExporterTurnedCountsInBudgetOff_ShowsFalse()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            Assert.False(body.GroupWithId(scenario.GroupId).GetProperty("countsInBudget").GetBoolean());
        }

        [Fact]
        public async Task Export_GroupWhereTheExporterKeptTheDefault_ShowsCountsInBudgetTrue()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            Assert.True(body.GroupWithId(scenario.LeftGroupId).GetProperty("countsInBudget").GetBoolean());
        }

        [Fact]
        public async Task Export_Membership_ShowsTheExportersOwnRowAndJoinTimeWithNoEnd()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement membership = body.GroupWithId(scenario.GroupId).GetProperty("membership");

            Assert.Equal(scenario.Group.OwnerRowId, membership.GetProperty("memberId").GetGuid());
            Assert.Equal(await ExportRows.MemberJoinedAtAsync(_app, scenario.Group.OwnerRowId), membership.GetProperty("joinedAt").GetDateTimeOffset());
            Assert.Null(ExportBody.StringOrNull(membership, "removedAt"));
            Assert.Null(ExportBody.StringOrNull(membership, "endKind"));
        }

        [Fact]
        public async Task Export_Members_AreEveryRowOfTheGroupIncludingTheRemovedPlainName()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            List<JsonElement> members = ExportBody.ListOf(body.GroupWithId(scenario.GroupId), "members");

            Guid[] expected = [scenario.Group.OwnerRowId, scenario.Group.MemberRowId, scenario.PlainRowId, scenario.RemovedPlainRowId];
            Assert.Equal(expected.Order(), ExportBody.IdsOf(members, "memberId").Order());
        }

        [Fact]
        public async Task Export_Members_ShowAnAccountMemberByTheirCurrentDisplayNameNotTheNameTheyJoinedWith()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement otherRow = MemberRow(body, scenario, scenario.Group.MemberRowId);

            Assert.Equal(ExportScenarioSteps.OtherDisplayName, otherRow.GetProperty("displayName").GetString());
            Assert.Equal("Bojan", await GroupRows.MemberRowValueAsync(_app, scenario.Group.MemberRowId, "name"));
        }

        [Fact]
        public async Task Export_Members_MarkOnlyTheExportersRowAsYou()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            List<JsonElement> members = ExportBody.ListOf(body.GroupWithId(scenario.GroupId), "members");

            JsonElement you = Assert.Single(members, member => member.GetProperty("isYou").GetBoolean());
            Assert.Equal(scenario.Group.OwnerRowId, you.GetProperty("memberId").GetGuid());
            Assert.Equal(GroupsApp.OwnerName, you.GetProperty("displayName").GetString());
        }

        [Fact]
        public async Task Export_Members_PlainNameMemberHasIsPlainNameTrueAndKeepsItsTypedName()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement plain = MemberRow(body, scenario, scenario.PlainRowId);

            Assert.True(plain.GetProperty("isPlainName").GetBoolean());
            Assert.False(plain.GetProperty("isYou").GetBoolean());
            Assert.Equal(ExportScenarioSteps.PlainName, plain.GetProperty("displayName").GetString());
            Assert.Equal(await ExportRows.MemberJoinedAtAsync(_app, scenario.PlainRowId), plain.GetProperty("joinedAt").GetDateTimeOffset());
        }

        [Fact]
        public async Task Export_Members_AccountMembersHaveIsPlainNameFalse()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            Assert.False(MemberRow(body, scenario, scenario.Group.OwnerRowId).GetProperty("isPlainName").GetBoolean());
            Assert.False(MemberRow(body, scenario, scenario.Group.MemberRowId).GetProperty("isPlainName").GetBoolean());
        }

        [Fact]
        public async Task Export_Members_RemovedPlainNameShowsEndKindAndRemovedAt()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement removed = MemberRow(body, scenario, scenario.RemovedPlainRowId);

            Assert.Equal("Removed", removed.GetProperty("endKind").GetString());
            Assert.Equal(ExpensesApp.ClockStart, removed.GetProperty("removedAt").GetDateTimeOffset());
            Assert.True(removed.GetProperty("isPlainName").GetBoolean());
            Assert.Equal(ExportScenarioSteps.RemovedPlainName, removed.GetProperty("displayName").GetString());
        }

        [Fact]
        public async Task Export_Members_ActiveMembersHaveNullEndKindAndRemovedAt()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement active = MemberRow(body, scenario, scenario.Group.MemberRowId);

            Assert.Null(ExportBody.StringOrNull(active, "endKind"));
            Assert.Null(ExportBody.StringOrNull(active, "removedAt"));
        }

        [Fact]
        public async Task Export_Expenses_AreTheTwoLiveOnesAndNotTheDeletedOne()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            List<JsonElement> expenses = ExportBody.ListOf(body.GroupWithId(scenario.GroupId), "expenses");

            Assert.Equal(
                new[] { scenario.DinnerExpenseId, scenario.TaxiExpenseId }.Order(),
                ExportBody.IdsOf(expenses).Order());
        }

        [Fact]
        public async Task Export_DeletedExpense_IsNoExpenseRowAndAppearsOnlyInItsAddedAndDeletedActivityRows()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement group = body.GroupWithId(scenario.GroupId);
            List<JsonElement> deletedExpenseRows = [.. ExportBody.ListOf(group, "activity").Where(row => ExportBody.GuidOrNull(row, "expenseId") == scenario.DeletedExpenseId)];

            Assert.DoesNotContain(scenario.DeletedExpenseId, ExportBody.IdsOf(ExportBody.ListOf(group, "expenses")));
            Assert.Equal(["ExpenseAdded", "ExpenseDeleted"], deletedExpenseRows.Select(row => row.GetProperty("type").GetString()).Order());
            Assert.All(deletedExpenseRows, row => Assert.Equal(ExportScenarioSteps.DeletedExpenseTitle, row.GetProperty("data").GetProperty("title").GetString()));
            Assert.Equal(2, OccurrencesIn(body.Raw, scenario.DeletedExpenseId.ToString()));
            Assert.Equal(2, OccurrencesIn(body.Raw, ExportScenarioSteps.DeletedExpenseTitle));
        }

        [Fact]
        public async Task Export_MkdExpense_ShowsTitleNoteAmountCurrencyDateCategoryAndPayer()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement dinner = ExpenseOf(body, scenario, scenario.DinnerExpenseId);

            Assert.Equal(ExportScenarioSteps.DinnerTitle, dinner.GetProperty("title").GetString());
            Assert.Equal(ExportScenarioSteps.DinnerNote, dinner.GetProperty("note").GetString());
            Assert.Equal(120_000, dinner.GetProperty("amountMinor").GetInt64());
            Assert.Equal("MKD", dinner.GetProperty("currency").GetString());
            Assert.Equal("2026-10-05", dinner.GetProperty("date").GetString());
            Assert.Equal("food", dinner.GetProperty("category").GetString());
            Assert.Equal(scenario.Group.OwnerRowId, dinner.GetProperty("paidByMemberId").GetGuid());
            Assert.Equal(ExpensesApp.ClockStart, dinner.GetProperty("createdAt").GetDateTimeOffset());
        }

        [Fact]
        public async Task Export_MkdExpense_ShowsTheEqualSplitWithAShareForEachMember()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement dinner = ExpenseOf(body, scenario, scenario.DinnerExpenseId);

            Assert.Equal("Equal", dinner.GetProperty("splitType").GetString());
            Dictionary<Guid, long> expectedShares = new()
            {
                [scenario.Group.OwnerRowId] = 40_000,
                [scenario.Group.MemberRowId] = 40_000,
                [scenario.PlainRowId] = 40_000,
            };
            Assert.Equal(expectedShares, SharesOf(dinner));
        }

        [Fact]
        public async Task Export_EurExpense_ShowsTheExactSplitInCentsWithNullTitleFieldsWhereNoneWasTyped()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement taxi = ExpenseOf(body, scenario, scenario.TaxiExpenseId);

            Assert.Equal(ExportScenarioSteps.TaxiTitle, taxi.GetProperty("title").GetString());
            Assert.Null(ExportBody.StringOrNull(taxi, "note"));
            Assert.Null(ExportBody.StringOrNull(taxi, "category"));
            Assert.Equal(3_000, taxi.GetProperty("amountMinor").GetInt64());
            Assert.Equal("EUR", taxi.GetProperty("currency").GetString());
            Assert.Equal("Exact", taxi.GetProperty("splitType").GetString());
            Assert.Equal(scenario.Group.MemberRowId, taxi.GetProperty("paidByMemberId").GetGuid());
            Dictionary<Guid, long> expectedShares = new()
            {
                [scenario.Group.OwnerRowId] = 1_000,
                [scenario.Group.MemberRowId] = 2_000,
            };
            Assert.Equal(expectedShares, SharesOf(taxi));
        }

        [Fact]
        public async Task Export_Payments_AreTheConfirmedAndThePendingOneAndNotTheDeletedOne()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            List<JsonElement> payments = ExportBody.ListOf(body.GroupWithId(scenario.GroupId), "payments");

            Assert.Equal(
                new[] { scenario.ConfirmedPaymentId, scenario.PendingPaymentId }.Order(),
                ExportBody.IdsOf(payments).Order());
            body.AssertDoesNotMention(scenario.DeletedPaymentId.ToString());
        }

        [Fact]
        public async Task Export_ConfirmedPayment_ShowsFromToAmountCurrencyStatusAndResolvedAt()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement payment = PaymentOf(body, scenario, scenario.ConfirmedPaymentId);

            Assert.Equal(scenario.Group.MemberRowId, payment.GetProperty("fromMemberId").GetGuid());
            Assert.Equal(scenario.Group.OwnerRowId, payment.GetProperty("toMemberId").GetGuid());
            Assert.Equal(2_500, payment.GetProperty("amountMinor").GetInt64());
            Assert.Equal("EUR", payment.GetProperty("currency").GetString());
            Assert.Equal("Confirmed", payment.GetProperty("status").GetString());
            Assert.NotNull(ExportBody.StringOrNull(payment, "resolvedAt"));
            Assert.Equal(ExpensesApp.ClockStart, payment.GetProperty("createdAt").GetDateTimeOffset());
        }

        [Fact]
        public async Task Export_PendingPayment_ShowsStatusPendingAndNullResolvedAt()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement payment = PaymentOf(body, scenario, scenario.PendingPaymentId);

            Assert.Equal("Pending", payment.GetProperty("status").GetString());
            Assert.Equal(30_000, payment.GetProperty("amountMinor").GetInt64());
            Assert.Equal("MKD", payment.GetProperty("currency").GetString());
            Assert.Null(ExportBody.StringOrNull(payment, "resolvedAt"));
        }

        [Fact]
        public async Task Export_DeletedGroup_IsNotListedAndNothingInsideItIsMentioned()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            Assert.DoesNotContain(scenario.DeletedGroupId, ExportBody.IdsOf(body.Groups));
            body.AssertDoesNotMention(scenario.DeletedGroupId.ToString());
            body.AssertDoesNotMention(ExportScenarioSteps.DeletedGroupName);
            body.AssertDoesNotMention(ExportScenarioSteps.DeletedGroupExpenseTitle);
            body.AssertDoesNotMention(scenario.DeletedGroupEventId.ToString());
        }

        [Fact]
        public async Task Export_LeftGroup_IsListedWithEndKindLeftAndRemovedAt()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement membership = body.GroupWithId(scenario.LeftGroupId).GetProperty("membership");

            Assert.Equal("Left", membership.GetProperty("endKind").GetString());
            Assert.Equal(ExpensesApp.ClockStart, membership.GetProperty("removedAt").GetDateTimeOffset());
        }

        [Fact]
        public async Task Export_LeftGroup_IsNotOwnedAndShowsItsOwnerByDisplayName()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement group = body.GroupWithId(scenario.LeftGroupId);

            Assert.False(group.GetProperty("isOwner").GetBoolean());
            Assert.Equal(ExportScenarioSteps.LeftGroupName, group.GetProperty("name").GetString());
            JsonElement ownerRow = Assert.Single(ExportBody.ListOf(group, "members"), member => member.GetProperty("displayName").GetString() == ExportScenarioSteps.OtherDisplayName);
            Assert.False(ownerRow.GetProperty("isYou").GetBoolean());
        }

        [Fact]
        public async Task Export_LeftGroup_ShowsTheExportersFormerRowAsYou()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement group = body.GroupWithId(scenario.LeftGroupId);

            JsonElement you = Assert.Single(ExportBody.ListOf(group, "members"), member => member.GetProperty("isYou").GetBoolean());
            Assert.Equal(group.GetProperty("membership").GetProperty("memberId").GetGuid(), you.GetProperty("memberId").GetGuid());
            Assert.Equal("Left", you.GetProperty("endKind").GetString());
        }

        [Fact]
        public async Task Export_GroupWithoutExpensesOrPayments_ShowsEmptyLists()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement group = body.GroupWithId(scenario.LeftGroupId);

            Assert.Empty(ExportBody.ListOf(group, "expenses"));
            Assert.Empty(ExportBody.ListOf(group, "payments"));
        }

        [Fact]
        public async Task Export_RemovedFromAGroup_ListsTheGroupWithEndKindRemoved()
        {
            SignedInUser exporter = await _app.SignUpAsync();
            Guid ownerId = await UserRows.AddAsync(_app);
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_app, ownerId, name: "Removed trip");
            await GroupRows.InsertMemberAsync(_app, groupId, exporter.UserId, exporter.Form.DisplayName, ownerId);
            await GroupRows.EndMembershipAsync(_app, groupId, exporter.UserId, "Removed");

            ExportBody body = await ExportRequests.GetBodyAsync(exporter.Client);

            JsonElement membership = body.GroupWithId(groupId).GetProperty("membership");
            Assert.Equal("Removed", membership.GetProperty("endKind").GetString());
            Assert.NotNull(ExportBody.StringOrNull(membership, "removedAt"));
        }

        private static JsonElement MemberRow(ExportBody body, ExportScenario scenario, Guid memberRowId)
        {
            return ExportBody.WithId(ExportBody.ListOf(body.GroupWithId(scenario.GroupId), "members"), memberRowId, "memberId");
        }

        private static JsonElement ExpenseOf(ExportBody body, ExportScenario scenario, Guid expenseId)
        {
            return ExportBody.WithId(ExportBody.ListOf(body.GroupWithId(scenario.GroupId), "expenses"), expenseId);
        }

        private static JsonElement PaymentOf(ExportBody body, ExportScenario scenario, Guid paymentId)
        {
            return ExportBody.WithId(ExportBody.ListOf(body.GroupWithId(scenario.GroupId), "payments"), paymentId);
        }

        private static int OccurrencesIn(string text, string value)
        {
            return text.Split(value, StringSplitOptions.None).Length - 1;
        }

        private static Dictionary<Guid, long> SharesOf(JsonElement expense)
        {
            return ExportBody.ListOf(expense, "shares")
                .ToDictionary(share => share.GetProperty("memberId").GetGuid(), share => share.GetProperty("amountMinor").GetInt64());
        }
    }
}
