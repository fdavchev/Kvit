using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Guards;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Kvit.Api.Tests.Settlements;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.BudgetShares
{
    public class ListGroupSharesTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task List_PersonInNoGroup_HasAnEmptyGroupSharesList()
        {
            SignedInUser user = await _app.SignUpAsync();

            JsonElement shares = await BudgetShareSteps.GroupSharesAsync(user.Client);

            Assert.Equal(JsonValueKind.Array, shares.ValueKind);
            Assert.Equal(0, shares.GetArrayLength());
        }

        [Fact]
        public async Task List_ShareOfAGroupExpense_HasTheDocumentedFieldsAndValues()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid food = await _app.BuiltInCategoryIdAsync("food");
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);

            JsonElement shares = await BudgetShareSteps.GroupSharesAsync(group.Member.Client);

            JsonElement row = Assert.Single(shares.EnumerateArray());
            Assert.Equal(BudgetShareSteps.GroupShareFieldNames, GroupRequests.PropertyNamesOf(row));
            Assert.Equal(expenseId, row.GetProperty("expenseId").GetGuid());
            Assert.Equal(group.GroupId, row.GetProperty("groupId").GetGuid());
            Assert.Equal(GroupRows.DefaultName, row.GetProperty("groupName").GetString());
            Assert.Equal(BudgetShareSteps.SharedTitle, row.GetProperty("title").GetString());
            Assert.Equal(food, row.GetProperty("categoryId").GetGuid());
            Assert.Equal(ExpenseInputs.DefaultDate, row.GetProperty("entryDate").GetString());
            Assert.Equal(ExpenseInputs.Mkd, row.GetProperty("currency").GetString());
            Assert.Equal(BudgetShareSteps.HalfOfOneThousandMkd, row.GetProperty("shareMinor").GetInt64());
        }

        [Fact]
        public async Task List_ExpenseSavedWithTheDefaultSwitch_IsListedAndStoresTrue()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);

            Assert.Equal([expenseId], await BudgetShareSteps.ListedExpenseIdsAsync(group.Member.Client));
            Assert.True(await BudgetShareSteps.ShareCountsAsync(_app, expenseId, group.MemberRowId));
        }

        [Fact]
        public async Task List_ExpenseSavedWhileTheSwitchIsOff_IsNotListedForThatPersonAndStillListedForTheOther()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetShareSteps.SetAsync(group.Member, group.GroupId, false);

            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);

            Assert.Empty(await BudgetShareSteps.ListedExpenseIdsAsync(group.Member.Client));
            Assert.Equal([expenseId], await BudgetShareSteps.ListedExpenseIdsAsync(group.Owner.Client));
            Assert.False(await BudgetShareSteps.ShareCountsAsync(_app, expenseId, group.MemberRowId));
            Assert.True(await BudgetShareSteps.ShareCountsAsync(_app, expenseId, group.OwnerRowId));
        }

        [Fact]
        public async Task List_ShareSavedBeforeTheSwitchWentOff_StaysListed()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);

            await BudgetShareSteps.SetAsync(group.Member, group.GroupId, false);

            Assert.Equal([expenseId], await BudgetShareSteps.ListedExpenseIdsAsync(group.Member.Client));
            Assert.True(await BudgetShareSteps.ShareCountsAsync(_app, expenseId, group.MemberRowId));
        }

        [Fact]
        public async Task List_SwitchBackOn_ListsOnlyTheExpensesSavedAfterwards()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid savedWhileOn = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            await BudgetShareSteps.SetAsync(group.Member, group.GroupId, false);
            Guid savedWhileOff = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            await BudgetShareSteps.SetAsync(group.Member, group.GroupId, true);
            Guid savedAfterwards = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);

            List<Guid> listed = await BudgetShareSteps.ListedExpenseIdsAsync(group.Member.Client);

            Assert.Equal(BudgetShareSteps.Sorted(savedWhileOn, savedAfterwards), listed);
            Assert.DoesNotContain(savedWhileOff, listed);
            Assert.False(await BudgetShareSteps.ShareCountsAsync(_app, savedWhileOff, group.MemberRowId));
        }

        [Fact]
        public async Task List_SwitchOfOneGroup_DoesNotHideTheSharesOfAnotherGroup()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid otherGroupId = await _app.CreateGroupAsync(group.Owner);
            Guid ownerRowInOtherGroup = await GroupRows.CurrentMemberIdAsync(_app, otherGroupId, group.Owner.UserId);
            Guid memberRowInOtherGroup = await GroupRows.InsertMemberAsync(_app, otherGroupId, group.Member.UserId, "Bojan", group.Owner.UserId);
            Guid food = await _app.BuiltInCategoryIdAsync("food");
            await BudgetShareSteps.SetAsync(group.Member, group.GroupId, false);

            Guid hidden = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            ExpenseInput otherInput = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, ownerRowInOtherGroup, ownerRowInOtherGroup, memberRowInOtherGroup) with { CategoryId = food };
            Guid listedExpense = await _app.AddExpenseAsync(group.Owner, otherGroupId, otherInput);

            List<Guid> listed = await BudgetShareSteps.ListedExpenseIdsAsync(group.Member.Client);

            Assert.Equal([listedExpense], listed);
            Assert.DoesNotContain(hidden, listed);
        }

        [Fact]
        public async Task List_DeletedExpense_IsNotListed()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid kept = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid deleted = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);

            HttpResponseMessage response = await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, deleted);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal([kept], await BudgetShareSteps.ListedExpenseIdsAsync(group.Member.Client));
            Assert.Equal([kept], await BudgetShareSteps.ListedExpenseIdsAsync(group.Owner.Client));
        }

        [Fact]
        public async Task List_RestoredExpense_IsListedAgain()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            HttpResponseMessage response = await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal([expenseId], await BudgetShareSteps.ListedExpenseIdsAsync(group.Member.Client));
        }

        [Fact]
        public async Task List_AnotherPersonsShare_IsNeverListed()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ShareInput[] shares = [new ShareInput(group.OwnerRowId, 70_000), new ShareInput(group.MemberRowId, 30_000)];
            ExpenseInput input = ExpenseInputs.Of("Exact", ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, shares) with { CategoryId = await _app.BuiltInCategoryIdAsync("food") };
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");

            JsonElement ownerRow = Assert.Single((await BudgetShareSteps.GroupSharesAsync(group.Owner.Client)).EnumerateArray());
            JsonElement memberRow = Assert.Single((await BudgetShareSteps.GroupSharesAsync(group.Member.Client)).EnumerateArray());

            Assert.Equal(expenseId, ownerRow.GetProperty("expenseId").GetGuid());
            Assert.Equal(70_000, ownerRow.GetProperty("shareMinor").GetInt64());
            Assert.Equal(expenseId, memberRow.GetProperty("expenseId").GetGuid());
            Assert.Equal(30_000, memberRow.GetProperty("shareMinor").GetInt64());
            Assert.Empty(await BudgetShareSteps.ListedExpenseIdsAsync(stranger.Client));
        }

        [Fact]
        public async Task List_PayerWhoOwesNothing_IsNotListedWhileTheOtherPersonIs()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ShareInput[] shares = [new ShareInput(group.OwnerRowId, 0), new ShareInput(group.MemberRowId, ExpenseInputs.OneThousandMkd)];
            ExpenseInput input = ExpenseInputs.Of("Exact", ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, shares) with { CategoryId = await _app.BuiltInCategoryIdAsync("food") };

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            Assert.Contains(new StoredShare(group.OwnerRowId, 0, 0), await ExpenseRows.SharesOfAsync(_app, expenseId));
            Assert.Empty(await BudgetShareSteps.ListedExpenseIdsAsync(group.Owner.Client));
            JsonElement memberRow = Assert.Single((await BudgetShareSteps.GroupSharesAsync(group.Member.Client)).EnumerateArray());
            Assert.Equal(ExpenseInputs.OneThousandMkd, memberRow.GetProperty("shareMinor").GetInt64());
        }

        [Fact]
        public async Task List_PayerWhoIsNotInTheSplit_IsNotListed()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.MemberRowId) with { CategoryId = await _app.BuiltInCategoryIdAsync("food") };

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            Assert.Empty(await BudgetShareSteps.ListedExpenseIdsAsync(group.Owner.Client));
            Assert.Equal([expenseId], await BudgetShareSteps.ListedExpenseIdsAsync(group.Member.Client));
        }

        [Fact]
        public async Task List_Settlements_NeverAppearAsShares()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId);
            await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId);

            Assert.Empty(await BudgetShareSteps.ListedExpenseIdsAsync(group.Owner.Client));
            Assert.Empty(await BudgetShareSteps.ListedExpenseIdsAsync(group.Member.Client));
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Assert.Equal([expenseId], await BudgetShareSteps.ListedExpenseIdsAsync(group.Member.Client));
            Assert.Equal([expenseId], await BudgetShareSteps.ListedExpenseIdsAsync(group.Owner.Client));
        }

        [Fact]
        public async Task List_SharesOfAPlainNameClaimedLater_AppearForTheClaimerWhetherSavedBeforeOrAfterTheClaim()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid markoRowId = group.PlainRowIds[0];
            Guid food = await _app.BuiltInCategoryIdAsync("food");
            ExpenseInput beforeInput = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, markoRowId) with { CategoryId = food };
            Guid savedBeforeTheClaim = await _app.AddExpenseAsync(group.Owner, group.GroupId, beforeInput);
            Assert.Empty(await BudgetShareSteps.ListedExpenseIdsAsync(group.Member.Client));

            HttpResponseMessage claimed = await GroupRequests.ClaimAsync(group.Member.Client, group.GroupId, markoRowId);
            Guid savedAfterTheClaim = await _app.AddExpenseAsync(group.Owner, group.GroupId, beforeInput with { Title = "Later" });

            Assert.Equal(HttpStatusCode.NoContent, claimed.StatusCode);
            Assert.Equal(BudgetShareSteps.Sorted(savedBeforeTheClaim, savedAfterTheClaim), await BudgetShareSteps.ListedExpenseIdsAsync(group.Member.Client));
            JsonElement older = BudgetShareSteps.RowOfExpense(await BudgetShareSteps.GroupSharesAsync(group.Member.Client), savedBeforeTheClaim);
            Assert.Equal(BudgetShareSteps.HalfOfOneThousandMkd, older.GetProperty("shareMinor").GetInt64());
            Assert.True(await BudgetShareSteps.ShareCountsAsync(_app, savedBeforeTheClaim, markoRowId));
        }

        [Fact]
        public async Task List_SharesOfAPlainNameThatIsNotClaimed_AppearForNobody()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.PlainRowIds[0]) with { CategoryId = await _app.BuiltInCategoryIdAsync("food") };
            await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            Assert.Empty(await BudgetShareSteps.ListedExpenseIdsAsync(group.Member.Client));
        }

        [Theory]
        [InlineData("Left")]
        [InlineData("Removed")]
        public async Task List_MemberWhoLeftOrWasRemoved_KeepsThePastSharesListed(string endKind)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, endKind);

            JsonElement shares = await BudgetShareSteps.GroupSharesAsync(group.Member.Client);

            JsonElement row = Assert.Single(shares.EnumerateArray());
            Assert.Equal(expenseId, row.GetProperty("expenseId").GetGuid());
            Assert.Equal(GroupRows.DefaultName, row.GetProperty("groupName").GetString());
            Assert.Equal(BudgetShareSteps.HalfOfOneThousandMkd, row.GetProperty("shareMinor").GetInt64());
        }

        [Fact]
        public async Task List_MkdAndEurShares_AreSeparateRowsEachInItsOwnCurrencyAndNeverConverted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid mkdExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, amountMinor: ExpenseInputs.OneThousandMkd, currency: ExpenseInputs.Mkd);
            Guid eurExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, amountMinor: 1_000, currency: ExpenseInputs.Eur);

            JsonElement shares = await BudgetShareSteps.GroupSharesAsync(group.Member.Client);

            Assert.Equal(2, shares.GetArrayLength());
            JsonElement mkdRow = BudgetShareSteps.RowOfExpense(shares, mkdExpense);
            Assert.Equal(ExpenseInputs.Mkd, mkdRow.GetProperty("currency").GetString());
            Assert.Equal(50_000, mkdRow.GetProperty("shareMinor").GetInt64());
            JsonElement eurRow = BudgetShareSteps.RowOfExpense(shares, eurExpense);
            Assert.Equal(ExpenseInputs.Eur, eurRow.GetProperty("currency").GetString());
            Assert.Equal(500, eurRow.GetProperty("shareMinor").GetInt64());
        }

        [Fact]
        public async Task List_FirstAndLastDayOfAMonth_BelongToThatMonthOnly()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid lastOfSeptember = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, "2026-09-30");
            Guid firstOfOctober = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, "2026-10-01");
            Guid lastOfOctober = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, "2026-10-31");
            Guid firstOfNovember = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, "2026-11-01");

            Assert.Equal(BudgetShareSteps.Sorted(firstOfOctober, lastOfOctober), await BudgetShareSteps.ListedExpenseIdsAsync(group.Member.Client, "2026-10"));
            Assert.Equal([lastOfSeptember], await BudgetShareSteps.ListedExpenseIdsAsync(group.Member.Client, "2026-09"));
            Assert.Equal([firstOfNovember], await BudgetShareSteps.ListedExpenseIdsAsync(group.Member.Client, "2026-11"));
        }

        [Fact]
        public async Task List_GroupShares_AreNotMixedIntoThePersonalEntriesOrTheirTotals()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid personalEntryId = await PersonalEntrySteps.AddAsync(group.Member.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, 70_000));
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);

            JsonElement body = await PersonalEntrySteps.MonthAsync(group.Member.Client);

            Assert.Equal([personalEntryId], await PersonalEntrySteps.EntryIdsOfMonthAsync(group.Member.Client));
            JsonElement mkd = Assert.Single(body.GetProperty("totals").EnumerateArray());
            Assert.Equal(70_000, mkd.GetProperty("totalMinor").GetInt64());
            Assert.Equal([expenseId], await BudgetShareSteps.ListedExpenseIdsAsync(group.Member.Client));
        }

        [Fact]
        public async Task List_GuestWithAShare_Answers403GuestNotAllowedAndNoShare()
        {
            (ExpenseGroup group, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.PlainRowIds[0]) with { CategoryId = await _app.BuiltInCategoryIdAsync("food") };
            await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            HttpResponseMessage response = await PersonalEntryRequests.ListAsync(guest.Client);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
        }

        [Fact]
        public async Task List_OneBillCreator_SeesTheirShareOfTheBill()
        {
            SignedInUser caller = await _app.SignUpAsync();
            Guid food = await _app.BuiltInCategoryIdAsync("food");
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko") with { CategoryId = food, Title = "Pizza" };

            (Guid groupId, Guid expenseId) = await _app.CreateOneBillAsync(caller, input);

            JsonElement row = Assert.Single((await BudgetShareSteps.GroupSharesAsync(caller.Client)).EnumerateArray());
            Assert.Equal(expenseId, row.GetProperty("expenseId").GetGuid());
            Assert.Equal(groupId, row.GetProperty("groupId").GetGuid());
            Assert.Equal(await GroupRows.GroupValueAsync(_app, groupId, "name"), row.GetProperty("groupName").GetString());
            Assert.Equal("Pizza", row.GetProperty("title").GetString());
            Assert.Equal(BudgetShareSteps.HalfOfOneThousandMkd, row.GetProperty("shareMinor").GetInt64());
            Assert.True(await BudgetShareSteps.ShareCountsAsync(_app, expenseId, await GroupRows.CurrentMemberIdAsync(_app, groupId, caller.UserId)));
        }

        [Theory]
        [InlineData("Closing")]
        [InlineData("Finished")]
        public async Task List_GroupThatIsClosingOrFinished_StillListsTheSharesOfEveryMember(string status)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);

            await GroupRows.SetStatusAsync(_app, group.GroupId, status);

            JsonElement memberRow = Assert.Single((await BudgetShareSteps.GroupSharesAsync(group.Member.Client)).EnumerateArray());
            JsonElement ownerRow = Assert.Single((await BudgetShareSteps.GroupSharesAsync(group.Owner.Client)).EnumerateArray());
            Assert.Equal(expenseId, memberRow.GetProperty("expenseId").GetGuid());
            Assert.Equal(BudgetShareSteps.HalfOfOneThousandMkd, memberRow.GetProperty("shareMinor").GetInt64());
            Assert.Equal(expenseId, ownerRow.GetProperty("expenseId").GetGuid());
            Assert.Equal(BudgetShareSteps.HalfOfOneThousandMkd, ownerRow.GetProperty("shareMinor").GetInt64());
        }

        [Fact]
        public async Task List_OneBillThatFinishedByItself_StillListsTheCreatorsShare()
        {
            OneBillSetup bill = await OneBillSteps.CreateBillAsync(_app);
            await OneBillSteps.MarkoPaysAsync(bill);
            await OneBillSteps.AssertFinishedNowByAsync(_app, bill.GroupId, bill.Owner);

            JsonElement row = Assert.Single((await BudgetShareSteps.GroupSharesAsync(bill.Owner.Client)).EnumerateArray());

            Assert.Equal(bill.ExpenseId, row.GetProperty("expenseId").GetGuid());
            Assert.Equal(bill.GroupId, row.GetProperty("groupId").GetGuid());
            Assert.Equal(OneBillSteps.MarkoShareMinor, row.GetProperty("shareMinor").GetInt64());
        }
    }
}
