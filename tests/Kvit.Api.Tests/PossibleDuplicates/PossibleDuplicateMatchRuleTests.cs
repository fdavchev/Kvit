using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.BudgetShares;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Xunit;

namespace Kvit.Api.Tests.PossibleDuplicates
{
    public class PossibleDuplicateMatchRuleTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Month_PersonWithNothing_HasAnEmptyPossibleDuplicatesListAndNotAMissingOne()
        {
            SignedInUser user = await _app.SignUpAsync();

            JsonElement pairs = await PossibleDuplicateSteps.PairsAsync(user.Client);

            Assert.Equal(JsonValueKind.Array, pairs.ValueKind);
            Assert.Equal(0, pairs.GetArrayLength());
        }

        [Fact]
        public async Task Month_EntryAndShareThatMatch_ListsOnePairWithTheDocumentedFieldsAndValues()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member, note: "Dinner out");

            JsonElement pairs = await PossibleDuplicateSteps.PairsAsync(group.Member.Client);

            JsonElement pair = Assert.Single(pairs.EnumerateArray());
            Assert.Equal(PossibleDuplicateSteps.PairFieldNames, GroupRequests.PropertyNamesOf(pair));
            Assert.Equal(entryId, pair.GetProperty("entryId").GetGuid());
            Assert.Equal("Dinner out", pair.GetProperty("entryNote").GetString());
            Assert.Equal(PossibleDuplicateSteps.ShareMinor, pair.GetProperty("entryAmountMinor").GetInt64());
            Assert.Equal(ExpenseInputs.DefaultDate, pair.GetProperty("entryDate").GetString());
            Assert.Equal(PossibleDuplicateSteps.Food, pair.GetProperty("entryCategoryId").GetGuid());
            Assert.Equal(expenseId, pair.GetProperty("expenseId").GetGuid());
            Assert.Equal(group.GroupId, pair.GetProperty("groupId").GetGuid());
            Assert.Equal(GroupRows.DefaultName, pair.GetProperty("groupName").GetString());
            Assert.Equal(BudgetShareSteps.SharedTitle, pair.GetProperty("expenseTitle").GetString());
            Assert.Equal(ExpenseInputs.Mkd, pair.GetProperty("currency").GetString());
            Assert.Equal(PossibleDuplicateSteps.ShareMinor, pair.GetProperty("shareMinor").GetInt64());
            Assert.Equal(PossibleDuplicateSteps.WholeMinor, pair.GetProperty("expenseAmountMinor").GetInt64());
            Assert.Equal(ExpenseInputs.DefaultDate, pair.GetProperty("expenseDate").GetString());
            Assert.Equal(PossibleDuplicateSteps.MatchedOnShare, pair.GetProperty("matchedOn").GetString());
        }

        [Fact]
        public async Task Month_EntryWithoutANoteAndExpenseWithoutATitle_HaveNullNoteAndTitle()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = await BudgetShareSteps.SharedInputAsync(_app, group) with { Title = null };
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);

            JsonElement pair = await PossibleDuplicateSteps.PairAsync(group.Member.Client, entryId, expenseId);

            Assert.Equal(JsonValueKind.Null, pair.GetProperty("entryNote").ValueKind);
            Assert.Equal(JsonValueKind.Null, pair.GetProperty("expenseTitle").ValueKind);
        }

        [Fact]
        public async Task Month_EntryAddedBeforeTheGroupExpense_ListsThePair()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);

            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);

            Assert.Equal(PossibleDuplicateSteps.Keys((entryId, expenseId)), await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Month_GroupExpenseAddedBeforeTheEntry_ListsThePair()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);

            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);

            Assert.Equal(PossibleDuplicateSteps.Keys((entryId, expenseId)), await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Theory]
        [InlineData("2026-10-05", "2026-10-05", true)]
        [InlineData("2026-10-02", "2026-10-05", true)]
        [InlineData("2026-10-01", "2026-10-05", false)]
        [InlineData("2026-10-05", "2026-10-08", true)]
        [InlineData("2026-10-05", "2026-10-09", false)]
        public async Task Month_DatesApart_MatchUpToExactlyThreeDaysEitherWayAndNotFour(string entryDate, string expenseDate, bool expectedPair)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, expenseDate);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member, entryDate: entryDate);

            List<(Guid EntryId, Guid ExpenseId)> pairs = await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client);

            Assert.Equal(expectedPair ? PossibleDuplicateSteps.Keys((entryId, expenseId)) : [], pairs);
        }

        [Theory]
        [InlineData(50_000, true)]
        [InlineData(55_000, true)]
        [InlineData(55_100, false)]
        [InlineData(45_000, true)]
        [InlineData(44_900, false)]
        [InlineData(100, false)]
        public async Task Month_EntryAmountAgainstTheShare_MatchesUpToTenPercentInclusive(long entryAmountMinor, bool expectedPair)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member, entryAmountMinor);

            List<(Guid EntryId, Guid ExpenseId)> pairs = await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client);

            Assert.Equal(expectedPair ? PossibleDuplicateSteps.Keys((entryId, expenseId)) : [], pairs);
        }

        [Theory]
        [InlineData(500, true)]
        [InlineData(550, true)]
        [InlineData(551, false)]
        [InlineData(450, true)]
        [InlineData(449, false)]
        public async Task Month_EuroEntryAgainstTheShare_MatchesUpToTenPercentInclusiveToTheLastCent(long entryAmountMinor, bool expectedPair)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, amountMinor: 1_000, currency: ExpenseInputs.Eur);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member, entryAmountMinor, currency: PersonalEntryRequests.Eur);

            List<(Guid EntryId, Guid ExpenseId)> pairs = await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client);

            Assert.Equal(expectedPair ? PossibleDuplicateSteps.Keys((entryId, expenseId)) : [], pairs);
        }

        [Theory]
        [InlineData(333, true)]
        [InlineData(366, true)]
        [InlineData(367, false)]
        [InlineData(300, true)]
        [InlineData(299, false)]
        public async Task Month_ShareThatIsNotARoundNumber_UsesWholeMinorUnitsForTheTenPercent(long entryAmountMinor, bool expectedPair)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ShareInput[] shares = [new ShareInput(group.OwnerRowId, 667), new ShareInput(group.MemberRowId, 333)];
            ExpenseInput input = ExpenseInputs.Of("Exact", 1_000, ExpenseInputs.Eur, group.OwnerRowId, shares) with { CategoryId = PossibleDuplicateSteps.Food };
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member, entryAmountMinor, currency: PersonalEntryRequests.Eur);

            List<(Guid EntryId, Guid ExpenseId)> pairs = await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client);

            Assert.Equal(expectedPair ? PossibleDuplicateSteps.Keys((entryId, expenseId)) : [], pairs);
        }

        [Theory]
        [InlineData(100_000, true)]
        [InlineData(110_000, true)]
        [InlineData(110_100, false)]
        [InlineData(90_000, true)]
        [InlineData(89_900, false)]
        public async Task Month_PayerEntryAgainstTheWholeBill_MatchesUpToTenPercentInclusiveAndSaysWhole(long entryAmountMinor, bool expectedPair)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Owner, entryAmountMinor);

            JsonElement pairs = await PossibleDuplicateSteps.PairsAsync(group.Owner.Client);

            if (expectedPair)
            {
                JsonElement pair = Assert.Single(pairs.EnumerateArray());
                Assert.Equal(entryId, pair.GetProperty("entryId").GetGuid());
                Assert.Equal(expenseId, pair.GetProperty("expenseId").GetGuid());
                Assert.Equal(PossibleDuplicateSteps.MatchedOnWhole, pair.GetProperty("matchedOn").GetString());
                Assert.Equal(PossibleDuplicateSteps.ShareMinor, pair.GetProperty("shareMinor").GetInt64());
                Assert.Equal(PossibleDuplicateSteps.WholeMinor, pair.GetProperty("expenseAmountMinor").GetInt64());
            }
            else
            {
                Assert.Equal(0, pairs.GetArrayLength());
            }
        }

        [Fact]
        public async Task Month_NonPayerWithAnEntryEqualToTheWholeBill_HasNoPair()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            await PossibleDuplicateSteps.AddEntryAsync(group.Member, PossibleDuplicateSteps.WholeMinor);

            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Month_PayerEntryEqualToTheirShare_IsMatchedOnShare()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Owner, PossibleDuplicateSteps.ShareMinor);

            JsonElement pair = await PossibleDuplicateSteps.PairAsync(group.Owner.Client, entryId, expenseId);

            Assert.Equal(PossibleDuplicateSteps.MatchedOnShare, pair.GetProperty("matchedOn").GetString());
        }

        [Fact]
        public async Task Month_PayerWhoIsAlsoTheOnlyPersonInTheSplit_IsMatchedOnShareNotWhole()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(PossibleDuplicateSteps.WholeMinor, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId) with { CategoryId = PossibleDuplicateSteps.Food };
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Owner, PossibleDuplicateSteps.WholeMinor);

            JsonElement pair = await PossibleDuplicateSteps.PairAsync(group.Owner.Client, entryId, expenseId);

            Assert.Equal(PossibleDuplicateSteps.MatchedOnShare, pair.GetProperty("matchedOn").GetString());
            Assert.Equal(PossibleDuplicateSteps.WholeMinor, pair.GetProperty("shareMinor").GetInt64());
        }

        [Fact]
        public async Task Month_EntryInAnotherCategory_HasNoPair()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            await PossibleDuplicateSteps.AddEntryAsync(group.Member, categoryId: PossibleDuplicateSteps.Transport);

            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Month_ExpenseWithoutACategory_NeverPairs()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = await BudgetShareSteps.SharedInputAsync(_app, group) with { CategoryId = null };
            await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AddEntryAsync(group.Owner, PossibleDuplicateSteps.WholeMinor);

            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Owner.Client));
        }

        [Fact]
        public async Task Month_EuroEntryWithTheSameNumberAsAMkdShare_HasNoPair()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            await PossibleDuplicateSteps.AddEntryAsync(group.Member, PossibleDuplicateSteps.ShareMinor, currency: PersonalEntryRequests.Eur);

            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Month_MkdEntryWithTheSameNumberAsAnEuroShare_HasNoPair()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetShareSteps.AddSharedExpenseAsync(_app, group, amountMinor: PossibleDuplicateSteps.WholeMinor, currency: ExpenseInputs.Eur);
            await PossibleDuplicateSteps.AddEntryAsync(group.Member, PossibleDuplicateSteps.ShareMinor, currency: PersonalEntryRequests.Mkd);

            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Month_EuroEntryAndEuroShare_MatchAndTheCurrencyIsEur()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, amountMinor: 1_000, currency: ExpenseInputs.Eur);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member, 500, currency: PersonalEntryRequests.Eur);

            JsonElement pair = await PossibleDuplicateSteps.PairAsync(group.Member.Client, entryId, expenseId);

            Assert.Equal(ExpenseInputs.Eur, pair.GetProperty("currency").GetString());
            Assert.Equal(500, pair.GetProperty("shareMinor").GetInt64());
            Assert.Equal(1_000, pair.GetProperty("expenseAmountMinor").GetInt64());
        }

        [Fact]
        public async Task Month_IncomeEntryWithTheSameAmountAndDate_HasNoPair()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            await PersonalEntrySteps.AddIncomeAsync(group.Member.Client, IncomeEntryInput.Of(PossibleDuplicateSteps.ShareMinor, entryDate: ExpenseInputs.DefaultDate));

            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Month_DeletedEntry_HasNoPairAndRestoringItBringsThePairBack()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PersonalEntryRequests.DeleteAsync(group.Member.Client, entryId);

            List<(Guid EntryId, Guid ExpenseId)> whileDeleted = await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client);
            await PersonalEntryRequests.RestoreAsync(group.Member.Client, entryId);
            List<(Guid EntryId, Guid ExpenseId)> afterRestore = await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client);

            Assert.Empty(whileDeleted);
            Assert.Equal(PossibleDuplicateSteps.Keys((entryId, expenseId)), afterRestore);
        }

        [Fact]
        public async Task Month_EntryEditedToAnAmountThatNoLongerMatches_LosesItsPair()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Assert.Single(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));

            await PersonalEntryRequests.EditAsync(group.Member.Client, entryId, EntryInput.Of(PossibleDuplicateSteps.Food, 90_000, entryDate: ExpenseInputs.DefaultDate));

            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Month_OneEntryThatMatchesTwoShares_ListsBothPairs()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid firstExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid secondExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, "2026-10-04");
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);

            List<(Guid EntryId, Guid ExpenseId)> pairs = await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client);

            Assert.Equal(PossibleDuplicateSteps.Keys((entryId, firstExpense), (entryId, secondExpense)), pairs);
        }

        [Fact]
        public async Task Month_OneShareThatMatchesTwoEntries_ListsBothPairs()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid firstEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid secondEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member, 52_000, "2026-10-04");

            List<(Guid EntryId, Guid ExpenseId)> pairs = await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client);

            Assert.Equal(PossibleDuplicateSteps.Keys((firstEntry, expenseId), (secondEntry, expenseId)), pairs);
        }

        [Fact]
        public async Task Month_EntryJustInsideTheMonthAndExpenseInThePreviousMonth_ListsThePairInTheEntrysMonthOnly()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, "2026-09-29");
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member, entryDate: "2026-10-01");

            List<(Guid EntryId, Guid ExpenseId)> inOctober = await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client, "2026-10");
            List<(Guid EntryId, Guid ExpenseId)> inSeptember = await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client, "2026-09");

            Assert.Equal(PossibleDuplicateSteps.Keys((entryId, expenseId)), inOctober);
            Assert.Empty(inSeptember);
        }

        [Fact]
        public async Task Month_EntryOnTheLastDayOfAMonthAndExpenseInTheNextMonth_ListsThePairInTheEntrysMonthOnly()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, "2026-10-02");
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member, entryDate: "2026-09-30");

            List<(Guid EntryId, Guid ExpenseId)> inSeptember = await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client, "2026-09");
            List<(Guid EntryId, Guid ExpenseId)> inOctober = await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client, "2026-10");

            Assert.Equal(PossibleDuplicateSteps.Keys((entryId, expenseId)), inSeptember);
            Assert.Empty(inOctober);
        }

        [Fact]
        public async Task Month_PairAcrossTheMonthBorderThreeDaysApart_Matches_AndFourDaysApart_DoesNot()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid threeDaysApart = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, "2026-09-28");
            await BudgetShareSteps.AddSharedExpenseAsync(_app, group, "2026-09-27");
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member, entryDate: "2026-10-01");

            List<(Guid EntryId, Guid ExpenseId)> pairs = await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client, "2026-10");

            Assert.Equal(PossibleDuplicateSteps.Keys((entryId, threeDaysApart)), pairs);
        }

        [Fact]
        public async Task Month_ExpenseOfAnotherPersonsGroup_NeverPairsWithAStrangersEntry()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");
            await PossibleDuplicateSteps.AddEntryAsync(stranger);

            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(stranger.Client));
        }
    }
}
