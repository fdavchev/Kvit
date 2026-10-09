using System.Text.Json;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class AddExpenseSplitTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Add_EqualWithAnExtra_HotelOf3000MkdAmongFive_GivesMarko1080AndTheOthers480()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko", "Petar", "Stefan");
            Guid marko = group.PlainRowIds[0];
            ShareInput[] shares = [.. group.RowIdsInJoiningOrder.Select(rowId => new ShareInput(rowId, rowId == marko ? 60_000 : 0))];
            ExpenseInput input = ExpenseInputs.Of("Equal", 300_000, ExpenseInputs.Mkd, group.OwnerRowId, shares);

            List<long> shareMinors = await AddAndReadSharesAsync(group, input);

            Assert.Equal([48_000L, 48_000L, 108_000L, 48_000L, 48_000L], shareMinors);
        }

        [Fact]
        public async Task Add_EqualOf1000MkdAmongThree_PayerInTheMiddle_GivesThePayer334()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.MemberRowId, group.OwnerRowId, group.MemberRowId, group.PlainRowIds[0]);

            List<long> shareMinors = await AddAndReadSharesAsync(group, input);

            Assert.Equal([33_300L, 33_400L, 33_300L], shareMinors);
        }

        [Fact]
        public async Task Add_EqualOf1000MkdAmongThree_PayerFirst_GivesThePayer334()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId, group.PlainRowIds[0]);

            List<long> shareMinors = await AddAndReadSharesAsync(group, input);

            Assert.Equal([33_400L, 33_300L, 33_300L], shareMinors);
        }

        [Fact]
        public async Task Add_EqualOf1000MkdAmongSeven_GivesThePayer148AndTheOthers142()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko", "Petar", "Stefan", "Jovan", "Viktor");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.PlainRowIds[0], [.. group.RowIdsInJoiningOrder]);

            List<long> shareMinors = await AddAndReadSharesAsync(group, input);

            Assert.Equal([14_200L, 14_200L, 14_800L, 14_200L, 14_200L, 14_200L, 14_200L], shareMinors);
        }

        [Fact]
        public async Task Add_SharesWithTheFirstPersonAtZero_PayerListedAtZero_StillTakesTheLeftover()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko", "Petar");
            ShareInput[] shares =
            [
                new ShareInput(group.OwnerRowId, 0),
                new ShareInput(group.MemberRowId, 1),
                new ShareInput(group.PlainRowIds[0], 1),
                new ShareInput(group.PlainRowIds[1], 1),
            ];
            ExpenseInput input = ExpenseInputs.Of("Shares", ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, shares);

            List<long> shareMinors = await AddAndReadSharesAsync(group, input);

            Assert.Equal([100L, 33_300L, 33_300L, 33_300L], shareMinors);
        }

        [Fact]
        public async Task Add_EqualOf10EurAmongThree_PayerInTheMiddle_GivesThePayer334Cents()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(1_000, ExpenseInputs.Eur, group.MemberRowId, group.OwnerRowId, group.MemberRowId, group.PlainRowIds[0]);

            List<long> shareMinors = await AddAndReadSharesAsync(group, input);

            Assert.Equal([333L, 334L, 333L], shareMinors);
        }

        [Fact]
        public async Task Add_EqualWithAnExtraInEur_GivesTheLeftoverToThePayer()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ShareInput[] shares = [new ShareInput(group.OwnerRowId, 0), new ShareInput(group.MemberRowId, 0), new ShareInput(group.PlainRowIds[0], 200)];
            ExpenseInput input = ExpenseInputs.Of("Equal", 1_000, ExpenseInputs.Eur, group.OwnerRowId, shares);

            List<long> shareMinors = await AddAndReadSharesAsync(group, input);

            Assert.Equal([268L, 266L, 466L], shareMinors);
        }

        [Fact]
        public async Task Add_ExactInMkd_StoresTheTypedAmountsAsTheShares()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ShareInput[] shares = [new ShareInput(group.OwnerRowId, 60_000), new ShareInput(group.MemberRowId, 30_000), new ShareInput(group.PlainRowIds[0], 10_000)];
            ExpenseInput input = ExpenseInputs.Of("Exact", ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, shares);

            List<long> shareMinors = await AddAndReadSharesAsync(group, input);

            Assert.Equal([60_000L, 30_000L, 10_000L], shareMinors);
        }

        [Fact]
        public async Task Add_ExactInEur_StoresTheTypedAmountsAsTheShares()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ShareInput[] shares = [new ShareInput(group.OwnerRowId, 1_000), new ShareInput(group.MemberRowId, 250)];
            ExpenseInput input = ExpenseInputs.Of("Exact", 1_250, ExpenseInputs.Eur, group.OwnerRowId, shares);

            List<long> shareMinors = await AddAndReadSharesAsync(group, input);

            Assert.Equal([1_000L, 250L], shareMinors);
        }

        [Fact]
        public async Task Add_PercentageInMkd_GivesThePayerTheLeftover()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ShareInput[] shares = [new ShareInput(group.OwnerRowId, 3_333), new ShareInput(group.MemberRowId, 3_333), new ShareInput(group.PlainRowIds[0], 3_334)];
            ExpenseInput input = ExpenseInputs.Of("Percentage", ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, shares);

            List<long> shareMinors = await AddAndReadSharesAsync(group, input);

            Assert.Equal([33_400L, 33_300L, 33_300L], shareMinors);
        }

        [Fact]
        public async Task Add_PercentageInEur_SplitsFiftyThirtyTwenty()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ShareInput[] shares = [new ShareInput(group.OwnerRowId, 5_000), new ShareInput(group.MemberRowId, 3_000), new ShareInput(group.PlainRowIds[0], 2_000)];
            ExpenseInput input = ExpenseInputs.Of("Percentage", 1_000, ExpenseInputs.Eur, group.OwnerRowId, shares);

            List<long> shareMinors = await AddAndReadSharesAsync(group, input);

            Assert.Equal([500L, 300L, 200L], shareMinors);
        }

        [Fact]
        public async Task Add_SharesInMkd_SplitsTwoToOneToOne()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ShareInput[] shares = [new ShareInput(group.OwnerRowId, 2), new ShareInput(group.MemberRowId, 1), new ShareInput(group.PlainRowIds[0], 1)];
            ExpenseInput input = ExpenseInputs.Of("Shares", ExpenseInputs.TwelveHundredMkd, ExpenseInputs.Mkd, group.OwnerRowId, shares);

            List<long> shareMinors = await AddAndReadSharesAsync(group, input);

            Assert.Equal([60_000L, 30_000L, 30_000L], shareMinors);
        }

        [Fact]
        public async Task Add_SharesInEur_GivesThePayerTheLeftoverCent()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ShareInput[] shares = [new ShareInput(group.OwnerRowId, 1), new ShareInput(group.MemberRowId, 1), new ShareInput(group.PlainRowIds[0], 1)];
            ExpenseInput input = ExpenseInputs.Of("Shares", 1_000, ExpenseInputs.Eur, group.MemberRowId, shares);

            List<long> shareMinors = await AddAndReadSharesAsync(group, input);

            Assert.Equal([333L, 334L, 333L], shareMinors);
        }

        [Fact]
        public async Task Add_PayerNotInTheSplit_GivesTheLeftoverToTheFirstPersonInJoiningOrderWhateverTheRequestOrder()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko", "Petar");
            ExpenseInput input = ExpenseInputs.Equal(
                ExpenseInputs.OneThousandMkd,
                ExpenseInputs.Mkd,
                group.OwnerRowId,
                group.PlainRowIds[1],
                group.PlainRowIds[0],
                group.MemberRowId);

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            JsonElement detail = await _app.ReadExpenseAsync(group.Owner, group.GroupId, expenseId);
            Assert.Equal([group.MemberRowId, group.PlainRowIds[0], group.PlainRowIds[1]], ExpenseJson.ShareMemberIds(detail));
            Assert.Equal([33_400L, 33_300L, 33_300L], ExpenseJson.ShareMinors(detail));
        }

        [Fact]
        public async Task Add_OnlySomeMembersInTheSplit_StoresOnlyTheirShares()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.MemberRowId, group.MemberRowId, group.PlainRowIds[0]);

            List<long> shareMinors = await AddAndReadSharesAsync(group, input);

            Assert.Equal([50_000L, 50_000L], shareMinors);
        }

        [Fact]
        public async Task Add_SplitWithOnlyThePayer_GivesThePayerTheWholeAmount()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId);

            List<long> shareMinors = await AddAndReadSharesAsync(group, input);

            Assert.Equal([ExpenseInputs.OneThousandMkd], shareMinors);
        }

        [Fact]
        public async Task Add_TypedInputValues_AreStoredAsTheyWereSentWhateverTheSplitType()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ShareInput[] shares = [new ShareInput(group.OwnerRowId, 5_000), new ShareInput(group.MemberRowId, 3_000), new ShareInput(group.PlainRowIds[0], 2_000)];
            ExpenseInput input = ExpenseInputs.Of("Percentage", ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, shares);

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            List<StoredShare> stored = await ExpenseRows.SharesOfAsync(_app, expenseId);
            Assert.Equal([5_000L, 3_000L, 2_000L], [.. stored.Select(share => share.InputValue)]);
            Assert.Equal([group.OwnerRowId, group.MemberRowId, group.PlainRowIds[0]], [.. stored.Select(share => share.MemberId)]);
        }

        [Fact]
        public async Task Add_EqualWithoutExtras_StoresZeroAsTheInputValueOfEveryone()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, [.. group.RowIdsInJoiningOrder]);

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            List<StoredShare> stored = await ExpenseRows.SharesOfAsync(_app, expenseId);
            Assert.Equal([0L, 0L, 0L], [.. stored.Select(share => share.InputValue)]);
        }

        private async Task<List<long>> AddAndReadSharesAsync(ExpenseGroup group, ExpenseInput input)
        {
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            JsonElement detail = await _app.ReadExpenseAsync(group.Owner, group.GroupId, expenseId);
            List<long> shareMinors = ExpenseJson.ShareMinors(detail);
            List<StoredShare> stored = await ExpenseRows.SharesOfAsync(_app, expenseId);
            Assert.Equal(shareMinors, [.. stored.Select(share => share.ShareMinor)]);
            Assert.Equal(input.AmountMinor, shareMinors.Sum());
            return shareMinors;
        }
    }
}
