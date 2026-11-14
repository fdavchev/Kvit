using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public class PurgeActivityNamesTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string OldName = "Zorica Petrovska";

        [Theory]
        [InlineData("MemberAdded")]
        [InlineData("MemberJoined")]
        [InlineData("MemberRemoved")]
        [InlineData("MemberLeft")]
        [InlineData("MemberLetBackIn")]
        [InlineData("GuestLetInAgain")]
        [InlineData("OwnershipTransferred")]
        [InlineData("MemberClaimed")]
        [InlineData("ClaimUndone")]
        public async Task Purge_FormerMember_ReplacesTheNameInTheDataOfMemberEvents(string type)
        {
            ExpenseGroup group = await GroupWithLeaverAsync();
            Guid eventId = await DeletionRows.InsertEventAsync(
                _app, group.GroupId, group.Owner.UserId, type, group.MemberRowId, $"{{\"name\": \"{OldName}\", \"claimedName\": \"Marko\"}}", null);

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.FormerMember);

            Assert.Equal(DeletionRequests.FormerMemberEnglish, await DeletionRows.EventValueAsync(_app, eventId, "data->>'name'"));
            Assert.Equal("Marko", await DeletionRows.EventValueAsync(_app, eventId, "data->>'claimedName'"));
        }

        [Theory]
        [InlineData("SettlementRecorded")]
        [InlineData("SettlementConfirmed")]
        [InlineData("SettlementRejected")]
        [InlineData("SettlementCancelled")]
        [InlineData("SettlementDeleted")]
        [InlineData("SettlementRestored")]
        public async Task Purge_FormerMember_ReplacesTheFromNameOfPaymentEventsAndLeavesTheOtherName(string type)
        {
            ExpenseGroup group = await GroupWithLeaverAsync();
            Guid eventId = await DeletionRows.InsertEventAsync(
                _app, group.GroupId, group.Owner.UserId, type, null, PaymentJson(OldName, "Ana"), null);

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.FormerMember);

            Assert.Equal(DeletionRequests.FormerMemberEnglish, await DeletionRows.EventValueAsync(_app, eventId, "data->>'fromName'"));
            Assert.Equal("Ana", await DeletionRows.EventValueAsync(_app, eventId, "data->>'toName'"));
            Assert.Equal("10000", await DeletionRows.EventValueAsync(_app, eventId, "data->>'amountMinor'"));
        }

        [Fact]
        public async Task Purge_FormerMember_ReplacesTheToNameOfAPaymentEvent()
        {
            ExpenseGroup group = await GroupWithLeaverAsync();
            Guid eventId = await DeletionRows.InsertEventAsync(
                _app, group.GroupId, group.Owner.UserId, "SettlementRecorded", null, PaymentJson("Ana", OldName), null);

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.FormerMember);

            Assert.Equal("Ana", await DeletionRows.EventValueAsync(_app, eventId, "data->>'fromName'"));
            Assert.Equal(DeletionRequests.FormerMemberEnglish, await DeletionRows.EventValueAsync(_app, eventId, "data->>'toName'"));
        }

        [Fact]
        public async Task Purge_FormerMember_ReplacesThePayerNameInAnEditedExpensesChanges()
        {
            ExpenseGroup group = await GroupWithLeaverAsync();
            Guid eventId = await DeletionRows.InsertEventAsync(
                _app, group.GroupId, group.Owner.UserId, "ExpenseEdited", null, "{\"currency\": \"MKD\"}", $"[{{\"field\": \"paidBy\", \"old\": \"{OldName}\", \"new\": \"Ana\"}}]");

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.FormerMember);

            Assert.Equal(DeletionRequests.FormerMemberEnglish, await DeletionRows.EventValueAsync(_app, eventId, "changes->0->>'old'"));
            Assert.Equal("Ana", await DeletionRows.EventValueAsync(_app, eventId, "changes->0->>'new'"));
            Assert.Equal("paidBy", await DeletionRows.EventValueAsync(_app, eventId, "changes->0->>'field'"));
        }

        [Fact]
        public async Task Purge_FormerMember_TheOldNameNeverSurvivesInsideASplitChangeText()
        {
            ExpenseGroup group = await GroupWithLeaverAsync();
            string changes = $"[{{\"field\": \"split\", \"old\": \"Equal: Ana 60000, {OldName} 60000\", \"new\": \"Equal: Ana 120000\"}}]";
            Guid eventId = await DeletionRows.InsertEventAsync(_app, group.GroupId, group.Owner.UserId, "ExpenseEdited", null, "{\"currency\": \"MKD\"}", changes);

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.FormerMember);

            string stored = await DeletionRows.EventValueAsync(_app, eventId, "changes") ?? string.Empty;
            Assert.DoesNotContain(OldName, stored);
            Assert.Contains("Equal: Ana 60000, Former member 60000", await DeletionRows.EventValueAsync(_app, eventId, "changes->0->>'old'"));
        }

        [Theory]
        [InlineData("Ana", "Equal: Ana 100, Anabel 100, Dana 100", "Equal: Former member 100, Anabel 100, Dana 100")]
        [InlineData("Ana", "Equal: Dana 100, Anabel 100, Ana 100", "Equal: Dana 100, Anabel 100, Former member 100")]
        [InlineData("Ana", "Equal: Ana 2 100, Ana 100", "Equal: Ana 2 100, Former member 100")]
        [InlineData("Equal", "Equal: Equal 100, Ana 100", "Equal: Former member 100, Ana 100")]
        [InlineData("Percentage", "Percentage: Ana 5000, Percentage 5000", "Percentage: Ana 5000, Former member 5000")]
        [InlineData("Ана", "Equal: Ана 100, Анабела 100, Дана 100", "Equal: Former member 100, Анабела 100, Дана 100")]
        [InlineData("Zorica Petrovska", "Exact: Zorica 100, Zorica Petrovska 200", "Exact: Zorica 100, Former member 200")]
        [InlineData("A.a (B+)?", "Exact: AXa B 100, A.a (B+)? 100", "Exact: AXa B 100, Former member 100")]
        [InlineData(@"Ana\d", @"Shares: Ana5 100, Ana\d 100", "Shares: Ana5 100, Former member 100")]
        public async Task Purge_FormerMember_ReplacesOnlyTheirWholeNameInASplitChangeText(string leaverName, string splitText, string expected)
        {
            ExpenseGroup group = await GroupWithLeaverAsync(leaverName);
            string changes = JsonSerializer.Serialize(new[] { new { field = "split", old = splitText, @new = splitText } });
            Guid eventId = await DeletionRows.InsertEventAsync(_app, group.GroupId, group.Owner.UserId, "ExpenseEdited", null, "{\"currency\": \"MKD\"}", changes);

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.FormerMember);

            Assert.Equal(expected, await DeletionRows.EventValueAsync(_app, eventId, "changes->0->>'old'"));
            Assert.Equal(expected, await DeletionRows.EventValueAsync(_app, eventId, "changes->0->>'new'"));
        }

        [Fact]
        public async Task Purge_FormerMember_TitleAndNoteChangesEqualToTheNameAreFreeTextAndStay()
        {
            ExpenseGroup group = await GroupWithLeaverAsync();
            string changes = $"[{{\"field\": \"title\", \"old\": \"{OldName}\", \"new\": \"Lunch\"}}, {{\"field\": \"note\", \"old\": \"\", \"new\": \"{OldName}\"}}]";
            Guid eventId = await DeletionRows.InsertEventAsync(_app, group.GroupId, group.Owner.UserId, "ExpenseEdited", null, "{\"currency\": \"MKD\"}", changes);

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.FormerMember);

            Assert.Equal(OldName, await DeletionRows.EventValueAsync(_app, eventId, "changes->0->>'old'"));
            Assert.Equal(OldName, await DeletionRows.EventValueAsync(_app, eventId, "changes->1->>'new'"));
        }

        [Fact]
        public async Task Purge_FormerMember_ExpenseTitleInTheDataOfAnExpenseEventStays()
        {
            ExpenseGroup group = await GroupWithLeaverAsync();
            Guid eventId = await DeletionRows.InsertEventAsync(
                _app, group.GroupId, group.Owner.UserId, "ExpenseAdded", null, $"{{\"title\": \"{OldName}\", \"amountMinor\": 1000, \"currency\": \"MKD\"}}", null);

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.FormerMember);

            Assert.Equal(OldName, await DeletionRows.EventValueAsync(_app, eventId, "data->>'title'"));
        }

        [Fact]
        public async Task Purge_FormerMember_OtherNamesInTheSameEventsStay()
        {
            ExpenseGroup group = await GroupWithLeaverAsync();
            Guid eventId = await DeletionRows.InsertEventAsync(
                _app, group.GroupId, group.Owner.UserId, "MemberAdded", group.PlainRowIds[0], "{\"name\": \"Marko\"}", null);

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.FormerMember);

            Assert.Equal("Marko", await DeletionRows.EventValueAsync(_app, eventId, "data->>'name'"));
        }

        [Fact]
        public async Task Purge_FormerMember_TheSameNameInAGroupTheyNeverJoined_IsNotReplaced()
        {
            ExpenseGroup group = await GroupWithLeaverAsync();
            SignedInUser strangerOwner = await _app.SignUpAsync("Stranger");
            DeletionGroup otherGroup = await DeletionGroups.CreateAsync(_app, strangerOwner);
            Guid eventId = await DeletionRows.InsertEventAsync(
                _app, otherGroup.GroupId, strangerOwner.UserId, "MemberAdded", null, $"{{\"name\": \"{OldName}\"}}", null);

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.FormerMember);

            Assert.Equal(OldName, await DeletionRows.EventValueAsync(_app, eventId, "data->>'name'"));
        }

        [Fact]
        public async Task Purge_FormerMember_AGroupTheyLeftEarlier_StillGetsItsEventsReplaced()
        {
            SignedInUser groupOwner = await _app.SignUpAsync("Ana");
            SignedInUser leaver = await _app.SignUpAsync(OldName);
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, groupOwner);
            Guid rowId = await DeletionGroups.AddEndedAccountAsync(_app, group, leaver, "Left", 5);
            Guid eventId = await DeletionRows.InsertEventAsync(
                _app, group.GroupId, leaver.UserId, "MemberLeft", rowId, $"{{\"name\": \"{OldName}\"}}", null);

            await DeletionSteps.RequestAndPurgeAsync(_app, leaver.Client, leaver.UserId, DeletionRequests.FormerMember);

            Assert.Equal(DeletionRequests.FormerMemberEnglish, await DeletionRows.EventValueAsync(_app, eventId, "data->>'name'"));
        }

        [Fact]
        public async Task Purge_FormerMemberWhoUsesMacedonian_TheStoredNameBecomesPoraneshenClen()
        {
            SignedInUser groupOwner = await _app.SignUpAsync("Ana");
            SignedInUser leaver = await _app.SignUpAsync(OldName);
            await AuthRequests.ChangeLanguageAsync(leaver.Client, "mk");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, groupOwner);
            Guid rowId = await DeletionGroups.AddAccountAsync(_app, group, leaver, 5);
            Guid eventId = await DeletionRows.InsertEventAsync(
                _app, group.GroupId, groupOwner.UserId, "MemberAdded", rowId, $"{{\"name\": \"{OldName}\"}}", null);

            await DeletionSteps.RequestAndPurgeAsync(_app, leaver.Client, leaver.UserId, DeletionRequests.FormerMember);

            Assert.Equal(DeletionRequests.FormerMemberMacedonian, await DeletionRows.EventValueAsync(_app, eventId, "data->>'name'"));
        }

        [Fact]
        public async Task Purge_TwoFormerMembersInOneGroup_NeitherOldNameSurvivesInTheEvents()
        {
            SignedInUser groupOwner = await _app.SignUpAsync("Ana");
            SignedInUser first = await _app.SignUpAsync("First Person");
            SignedInUser second = await _app.SignUpAsync("Second Person");
            DeletionGroup group = await DeletionGroups.CreateAsync(_app, groupOwner);
            Guid firstRowId = await DeletionGroups.AddAccountAsync(_app, group, first, 5);
            Guid secondRowId = await DeletionGroups.AddAccountAsync(_app, group, second, 6);
            Guid firstEvent = await DeletionRows.InsertEventAsync(_app, group.GroupId, groupOwner.UserId, "MemberAdded", firstRowId, "{\"name\": \"First Person\"}", null);
            Guid secondEvent = await DeletionRows.InsertEventAsync(_app, group.GroupId, groupOwner.UserId, "MemberAdded", secondRowId, "{\"name\": \"Second Person\"}", null);

            await DeletionSteps.RequestAndPurgeAsync(_app, first.Client, first.UserId, DeletionRequests.FormerMember);
            await DeletionSteps.RequestAndPurgeAsync(_app, second.Client, second.UserId, DeletionRequests.FormerMember);

            string? firstName = await DeletionRows.EventValueAsync(_app, firstEvent, "data->>'name'");
            string? secondName = await DeletionRows.EventValueAsync(_app, secondEvent, "data->>'name'");
            Assert.StartsWith(DeletionRequests.FormerMemberEnglish, firstName, StringComparison.Ordinal);
            Assert.StartsWith(DeletionRequests.FormerMemberEnglish, secondName, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Purge_KeepName_NothingStoredInTheActivityOfTheGroupChanges()
        {
            ExpenseGroup group = await GroupWithLeaverAsync();
            await DeletionRows.InsertEventAsync(_app, group.GroupId, group.Owner.UserId, "MemberAdded", group.MemberRowId, $"{{\"name\": \"{OldName}\"}}", null);
            await DeletionRows.InsertEventAsync(_app, group.GroupId, group.Owner.UserId, "SettlementRecorded", null, PaymentJson(OldName, "Ana"), null);
            await DeletionRows.InsertEventAsync(_app, group.GroupId, group.Owner.UserId, "ExpenseEdited", null, "{\"currency\": \"MKD\"}", $"[{{\"field\": \"paidBy\", \"old\": \"{OldName}\", \"new\": \"Ana\"}}]");
            string before = await DeletionRows.SnapshotOfTablesAsync(_app, "activity_events");

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.KeepName);

            Assert.Equal(before, await DeletionRows.SnapshotOfTablesAsync(_app, "activity_events"));
        }

        [Fact]
        public async Task Purge_FormerMember_EventsOfOtherTypesAndOtherGroupsKeepTheirRowCount()
        {
            ExpenseGroup group = await GroupWithLeaverAsync();
            await DeletionRows.InsertEventAsync(_app, group.GroupId, group.Owner.UserId, "MemberAdded", group.MemberRowId, $"{{\"name\": \"{OldName}\"}}", null);
            int eventsBefore = await GroupRows.CountEventsAsync(_app, group.GroupId);

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.FormerMember);

            Assert.Equal(eventsBefore, await GroupRows.CountEventsAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Purge_FormerMember_TheActivityFeedAnswerNoLongerContainsTheOldName()
        {
            ExpenseGroup group = await GroupWithLeaverAsync();
            await DeletionRows.InsertEventAsync(_app, group.GroupId, group.Member.UserId, "MemberJoined", group.MemberRowId, $"{{\"name\": \"{OldName}\"}}", null);
            await DeletionRows.InsertEventAsync(_app, group.GroupId, group.Owner.UserId, "SettlementRecorded", null, PaymentJson(OldName, "Ana"), null);

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.FormerMember);

            HttpResponseMessage response = await ExpenseRequests.ActivityAsync(group.Owner.Client, group.GroupId);
            string text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.DoesNotContain(OldName, text);
            Assert.Contains(DeletionRequests.FormerMemberEnglish, text);
            JsonElement events = (await AuthRequests.ReadJsonAsync(await ExpenseRequests.ActivityAsync(group.Owner.Client, group.GroupId))).GetProperty("events");
            Assert.True(events.GetArrayLength() >= 3);
        }

        private async Task<ExpenseGroup> GroupWithLeaverAsync(string leaverName = OldName)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await UserRows.SetDisplayNameAsync(_app, group.Member.UserId, leaverName);

            return group;
        }

        private static string PaymentJson(string fromName, string toName)
        {
            return JsonSerializer.Serialize(new { amountMinor = 10000, currency = "MKD", fromName, toName });
        }
    }
}
