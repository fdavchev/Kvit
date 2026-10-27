using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.BudgetShares;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.PossibleDuplicates
{
    public class PossibleDuplicateSharesTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Month_DeletedGroupExpense_HasNoPairAndRestoringItBringsThePairBack()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            List<(Guid EntryId, Guid ExpenseId)> whileDeleted = await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client);
            await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId);
            List<(Guid EntryId, Guid ExpenseId)> afterRestore = await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client);

            Assert.Empty(whileDeleted);
            Assert.Equal(PossibleDuplicateSteps.Keys((entryId, expenseId)), afterRestore);
        }

        [Fact]
        public async Task Month_ExpenseOfADeletedGroup_HasNoPair()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await GroupRows.SetDeletedAsync(_app, group.GroupId, group.Owner.UserId, 1);

            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Month_ShareSavedWhileTheBudgetSwitchWasOff_HasNoPairForThatPersonAndOneForTheOther()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetShareSteps.SetAsync(group.Member, group.GroupId, false);
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid ownerEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Owner);

            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
            Assert.Equal(PossibleDuplicateSteps.Keys((ownerEntry, expenseId)), await PossibleDuplicateSteps.PairKeysAsync(group.Owner.Client));
        }

        [Fact]
        public async Task Month_ShareSavedBeforeTheBudgetSwitchWentOff_StillPairs()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);

            await BudgetShareSteps.SetAsync(group.Member, group.GroupId, false);

            Assert.Equal(PossibleDuplicateSteps.Keys((entryId, expenseId)), await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Month_PayerWhoseShareIsZero_HasNoPairEvenForAnEntryEqualToTheWholeBill()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ShareInput[] shares = [new ShareInput(group.OwnerRowId, 0), new ShareInput(group.MemberRowId, PossibleDuplicateSteps.WholeMinor)];
            ExpenseInput input = ExpenseInputs.Of("Exact", PossibleDuplicateSteps.WholeMinor, ExpenseInputs.Mkd, group.OwnerRowId, shares) with { CategoryId = PossibleDuplicateSteps.Food };
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await PossibleDuplicateSteps.AddEntryAsync(group.Owner, PossibleDuplicateSteps.WholeMinor);
            Guid memberEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member, PossibleDuplicateSteps.WholeMinor);

            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Owner.Client));
            Assert.Equal(PossibleDuplicateSteps.Keys((memberEntry, expenseId)), await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Month_PayerWhoIsNotInTheSplit_HasNoPairEvenForAnEntryEqualToTheWholeBill()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(PossibleDuplicateSteps.WholeMinor, ExpenseInputs.Mkd, group.OwnerRowId, group.MemberRowId) with { CategoryId = PossibleDuplicateSteps.Food };
            await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await PossibleDuplicateSteps.AddEntryAsync(group.Owner, PossibleDuplicateSteps.WholeMinor);

            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Owner.Client));
        }

        [Fact]
        public async Task Month_AnotherPersonsShare_NeverPairsWithMyEntry()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ShareInput[] shares = [new ShareInput(group.OwnerRowId, 70_000), new ShareInput(group.MemberRowId, 30_000)];
            ExpenseInput input = ExpenseInputs.Of("Exact", PossibleDuplicateSteps.WholeMinor, ExpenseInputs.Mkd, group.OwnerRowId, shares) with { CategoryId = PossibleDuplicateSteps.Food };
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await PossibleDuplicateSteps.AddEntryAsync(group.Member, 70_000);
            Guid memberEntryOfHisShare = await PossibleDuplicateSteps.AddEntryAsync(group.Member, 30_000);
            Guid ownerEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Owner, 70_000);

            List<(Guid EntryId, Guid ExpenseId)> memberPairs = await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client);
            List<(Guid EntryId, Guid ExpenseId)> ownerPairs = await PossibleDuplicateSteps.PairKeysAsync(group.Owner.Client);

            Assert.Equal(PossibleDuplicateSteps.Keys((memberEntryOfHisShare, expenseId)), memberPairs);
            Assert.Equal(PossibleDuplicateSteps.Keys((ownerEntry, expenseId)), ownerPairs);
        }

        [Fact]
        public async Task Month_PersonWhoIsNotInTheGroup_HasNoPair()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");
            await PossibleDuplicateSteps.AddEntryAsync(stranger);

            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(stranger.Client));
        }

        [Fact]
        public async Task Month_ShareOfAPlainNameThatIsNotClaimed_HasNoPair()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(PossibleDuplicateSteps.WholeMinor, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.PlainRowIds[0]) with { CategoryId = PossibleDuplicateSteps.Food };
            await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await PossibleDuplicateSteps.AddEntryAsync(group.Member);

            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Month_OlderShareOfAPlainNameClaimedLater_PairsWithTheClaimersEntry()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid markoRowId = group.PlainRowIds[0];
            ExpenseInput input = ExpenseInputs.Equal(PossibleDuplicateSteps.WholeMinor, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, markoRowId) with { CategoryId = PossibleDuplicateSteps.Food };
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));

            HttpResponseMessage claimed = await GroupRequests.ClaimAsync(group.Member.Client, group.GroupId, markoRowId);

            Assert.Equal(HttpStatusCode.NoContent, claimed.StatusCode);
            Assert.Equal(PossibleDuplicateSteps.Keys((entryId, expenseId)), await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Month_BillPaidByAPlainNameClaimedLater_PairsTheClaimersWholeBillEntryAsWhole()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid markoRowId = group.PlainRowIds[0];
            ExpenseInput input = ExpenseInputs.Equal(PossibleDuplicateSteps.WholeMinor, ExpenseInputs.Mkd, markoRowId, group.OwnerRowId, markoRowId) with { CategoryId = PossibleDuplicateSteps.Food };
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member, PossibleDuplicateSteps.WholeMinor);
            await GroupRequests.ClaimAsync(group.Member.Client, group.GroupId, markoRowId);

            JsonElement pair = await PossibleDuplicateSteps.PairAsync(group.Member.Client, entryId, expenseId);

            Assert.Equal(PossibleDuplicateSteps.MatchedOnWhole, pair.GetProperty("matchedOn").GetString());
        }

        [Theory]
        [InlineData("Left")]
        [InlineData("Removed")]
        public async Task Month_MemberWhoLeftOrWasRemoved_StillPairsWithThePastShare(string endKind)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, endKind);

            List<(Guid EntryId, Guid ExpenseId)> pairs = await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client);

            Assert.Equal(PossibleDuplicateSteps.Keys((entryId, expenseId)), pairs);
        }

        [Theory]
        [InlineData("Closing")]
        [InlineData("Finished")]
        public async Task Month_GroupThatIsClosingOrFinished_StillListsThePairForEveryMember(string status)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid memberEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid ownerEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Owner);

            await GroupRows.SetStatusAsync(_app, group.GroupId, status);

            Assert.Equal(PossibleDuplicateSteps.Keys((memberEntry, expenseId)), await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
            Assert.Equal(PossibleDuplicateSteps.Keys((ownerEntry, expenseId)), await PossibleDuplicateSteps.PairKeysAsync(group.Owner.Client));
        }

        [Fact]
        public async Task Month_ShareOfAOneBill_PairsWithTheCreatorsEntry()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(PossibleDuplicateSteps.WholeMinor, ExpenseInputs.Mkd, "Marko") with { CategoryId = PossibleDuplicateSteps.Food, Title = "Pizza" };
            (Guid groupId, Guid expenseId) = await _app.CreateOneBillAsync(caller, input);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(caller);

            JsonElement pair = await PossibleDuplicateSteps.PairAsync(caller.Client, entryId, expenseId);

            Assert.Equal(groupId, pair.GetProperty("groupId").GetGuid());
            Assert.Equal("Pizza", pair.GetProperty("expenseTitle").GetString());
        }
    }
}
