using System.Globalization;
using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class AddExpenseValidationTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData("mkd")]
        [InlineData("eur")]
        [InlineData("Mkd")]
        [InlineData("USD")]
        [InlineData("")]
        [InlineData(" MKD")]
        public async Task Add_CurrencyOutsideTheTwoKnownNames_Answers400CurrencyInvalid(string currency)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            await AssertRejectedAsync(group, ValidInput(group) with { Currency = currency }, ResultCodes.EXPENSE_CURRENCY_INVALID);
        }

        [Theory]
        [InlineData("equal")]
        [InlineData("EQUAL")]
        [InlineData("exact")]
        [InlineData("Unknown")]
        [InlineData("")]
        public async Task Add_SplitTypeNotSpelledExactly_Answers400SplitTypeInvalid(string splitType)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            await AssertRejectedAsync(group, ValidInput(group) with { SplitType = splitType }, ResultCodes.EXPENSE_SPLIT_TYPE_INVALID);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100_000)]
        public async Task Add_AmountOfZeroOrLess_Answers400AmountNotPositive(long amountMinor)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            await AssertRejectedAsync(group, ValidInput(group) with { AmountMinor = amountMinor }, ResultCodes.EXPENSE_AMOUNT_NOT_POSITIVE);
        }

        [Theory]
        [InlineData("MKD", 1_000_000_000_000)]
        [InlineData("EUR", 1_000_000_000_000)]
        [InlineData("EUR", 1_000_000_000_001)]
        [InlineData("EUR", long.MaxValue)]
        public async Task Add_AmountAbove999999999999MinorUnits_Answers400AmountTooLarge(string currency, long amountMinor)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            await AssertRejectedAsync(group, ValidInput(group) with { Currency = currency, AmountMinor = amountMinor }, ResultCodes.EXPENSE_AMOUNT_TOO_LARGE);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(99)]
        [InlineData(150)]
        [InlineData(12_345)]
        public async Task Add_MkdAmountNotAWholeDenar_Answers400NotOnCurrencyStep(long amountMinor)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            await AssertRejectedAsync(group, ValidInput(group) with { AmountMinor = amountMinor }, ResultCodes.MONEY_NOT_ON_CURRENCY_STEP);
        }

        [Fact]
        public async Task Add_EurAmountWithCents_IsAccepted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(1_234, ExpenseInputs.Eur, group.OwnerRowId, group.OwnerRowId);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Add_EurAmountOf999999999999Cents_IsAccepted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(999_999_999_999, ExpenseInputs.Eur, group.OwnerRowId, group.OwnerRowId);

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            Assert.Equal("999999999999", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "amount_minor"));
            List<StoredShare> shares = await ExpenseRows.SharesOfAsync(_app, expenseId);
            Assert.Equal([999_999_999_999L], [.. shares.Select(share => share.ShareMinor)]);
        }

        [Fact]
        public async Task Add_MkdAmountOfTheLargestWholeDenarBelowTheLimit_IsAccepted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(999_999_999_900, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId);

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            Assert.Equal("999999999900", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "amount_minor"));
        }

        [Fact]
        public async Task Add_DateBefore2000_Answers400DateInvalid()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            await AssertRejectedAsync(group, ValidInput(group) with { ExpenseDate = "1999-12-31" }, ResultCodes.EXPENSE_DATE_INVALID);
        }

        [Fact]
        public async Task Add_DateOn20000101_IsAccepted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ValidInput(group) with { ExpenseDate = "2000-01-01" });

            Assert.Equal("2000-01-01", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "expense_date"));
        }

        [Fact]
        public async Task Add_DateOneDayPast366DaysAhead_Answers400DateInvalid()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            string tooFar = _app.Clock.UtcToday.AddDays(367).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            await AssertRejectedAsync(group, ValidInput(group) with { ExpenseDate = tooFar }, ResultCodes.EXPENSE_DATE_INVALID);
        }

        [Fact]
        public async Task Add_Date366DaysAhead_IsAccepted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            string farthest = _app.Clock.UtcToday.AddDays(366).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ValidInput(group) with { ExpenseDate = farthest });

            Assert.Equal(farthest, await ExpenseRows.ExpenseValueAsync(_app, expenseId, "expense_date"));
        }

        [Fact]
        public async Task Add_DateTodayInUtc_IsAccepted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            string today = _app.Clock.UtcToday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ValidInput(group) with { ExpenseDate = today });

            Assert.Equal(today, await ExpenseRows.ExpenseValueAsync(_app, expenseId, "expense_date"));
        }

        [Theory]
        [InlineData(81)]
        [InlineData(200)]
        public async Task Add_TitleLongerThan80Characters_Answers400TitleInvalid(int length)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            await AssertRejectedAsync(group, ValidInput(group) with { Title = ExpenseInputs.TextOfLength(length) }, ResultCodes.EXPENSE_TITLE_INVALID);
        }

        [Fact]
        public async Task Add_TitleLongerThan80CharactersAfterTrimming_Answers400TitleInvalid()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            string title = $"  {ExpenseInputs.TextOfLength(81)}  ";

            await AssertRejectedAsync(group, ValidInput(group) with { Title = title }, ResultCodes.EXPENSE_TITLE_INVALID);
        }

        [Fact]
        public async Task Add_NoteLongerThan500Characters_Answers400NoteInvalid()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            await AssertRejectedAsync(group, ValidInput(group) with { Note = ExpenseInputs.TextOfLength(501) }, ResultCodes.EXPENSE_NOTE_INVALID);
        }

        [Fact]
        public async Task Add_CategoryThatDoesNotExist_Answers400CategoryInvalid()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            await AssertRejectedAsync(group, ValidInput(group) with { CategoryId = Guid.CreateVersion7() }, ResultCodes.EXPENSE_CATEGORY_INVALID);
        }

        [Fact]
        public async Task Add_CustomCategoryOfSomeone_Answers400CategoryInvalid()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid custom = await InsertCategoryAsync(group.Owner.UserId, key: null, name: "Pets", isArchived: false);

            await AssertRejectedAsync(group, ValidInput(group) with { CategoryId = custom }, ResultCodes.EXPENSE_CATEGORY_INVALID);
        }

        [Fact]
        public async Task Add_ArchivedBuiltInCategory_Answers400CategoryInvalid()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid archived = await InsertCategoryAsync(null, key: $"retired-{Guid.NewGuid():N}", name: null, isArchived: true);

            await AssertRejectedAsync(group, ValidInput(group) with { CategoryId = archived }, ResultCodes.EXPENSE_CATEGORY_INVALID);
        }

        [Fact]
        public async Task Add_ExactThatDoesNotAddUp_Answers400SplitDoesNotAddUp()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Of("Exact", ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, new ShareInput(group.OwnerRowId, 60_000), new ShareInput(group.MemberRowId, 30_000));

            await AssertRejectedAsync(group, input, ResultCodes.EXPENSE_SPLIT_DOES_NOT_ADD_UP);
        }

        [Fact]
        public async Task Add_ExactThatAddsUpToMoreThanTheTotal_Answers400SplitDoesNotAddUp()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Of("Exact", ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, new ShareInput(group.OwnerRowId, 60_000), new ShareInput(group.MemberRowId, 50_000));

            await AssertRejectedAsync(group, input, ResultCodes.EXPENSE_SPLIT_DOES_NOT_ADD_UP);
        }

        [Fact]
        public async Task Add_PercentagesThatDoNotTotal100_Answers400SplitDoesNotAddUp()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ShareInput[] shares = [new ShareInput(group.OwnerRowId, 3_333), new ShareInput(group.MemberRowId, 3_333), new ShareInput(group.PlainRowIds[0], 3_333)];
            ExpenseInput input = ExpenseInputs.Of("Percentage", ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, shares);

            await AssertRejectedAsync(group, input, ResultCodes.EXPENSE_SPLIT_DOES_NOT_ADD_UP);
        }

        [Fact]
        public async Task Add_SharesThatAreAllZero_Answers400SplitNoShares()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Of("Shares", ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, new ShareInput(group.OwnerRowId, 0), new ShareInput(group.MemberRowId, 0));

            await AssertRejectedAsync(group, input, ResultCodes.EXPENSE_SPLIT_NO_SHARES);
        }

        [Fact]
        public async Task Add_NoSharesAtAll_Answers400SplitNoParticipants()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Of("Equal", ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId);

            await AssertRejectedAsync(group, input, ResultCodes.EXPENSE_SPLIT_NO_PARTICIPANTS);
        }

        [Fact]
        public async Task Add_TheSameMemberTwiceInTheSplit_Answers400SplitDuplicateMember()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.OwnerRowId);

            await AssertRejectedAsync(group, input, ResultCodes.EXPENSE_SPLIT_DUPLICATE_MEMBER);
        }

        [Fact]
        public async Task Add_NegativeInputValue_Answers400SplitNegativeInput()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Of("Shares", ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, new ShareInput(group.OwnerRowId, 2), new ShareInput(group.MemberRowId, -1));

            await AssertRejectedAsync(group, input, ResultCodes.EXPENSE_SPLIT_NEGATIVE_INPUT);
        }

        [Fact]
        public async Task Add_EqualExtrasLargerThanTheTotal_Answers400SplitExtrasExceedTotal()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Of("Equal", ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, new ShareInput(group.OwnerRowId, 120_000), new ShareInput(group.MemberRowId, 0));

            await AssertRejectedAsync(group, input, ResultCodes.EXPENSE_SPLIT_EXTRAS_EXCEED_TOTAL);
        }

        [Fact]
        public async Task Add_ExactAmountNotAWholeDenar_Answers400NotOnCurrencyStep()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Of("Exact", ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, new ShareInput(group.OwnerRowId, 50_050), new ShareInput(group.MemberRowId, 49_950));

            await AssertRejectedAsync(group, input, ResultCodes.MONEY_NOT_ON_CURRENCY_STEP);
        }

        private static ExpenseInput ValidInput(ExpenseGroup group)
        {
            return ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
        }

        private async Task AssertRejectedAsync(ExpenseGroup group, ExpenseInput input, string expectedErrorCode)
        {
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, expectedErrorCode);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(0, await GroupRows.CountUsageEventsAsync(_app, group.Owner.UserId, "ExpenseAdded"));
        }

        private async Task<Guid> InsertCategoryAsync(Guid? ownerUserId, string? key, string? name, bool isArchived)
        {
            Guid categoryId = Guid.CreateVersion7();
            await _app.ExecuteAsync(
                "INSERT INTO categories (id, owner_user_id, key, name, emoji, color, sort_order, archived_at) "
                + "VALUES (@id, @owner, @key, @name, @emoji, @color, @sort_order, @archived_at)",
                new NpgsqlParameter("id", categoryId),
                new NpgsqlParameter("owner", (object?)ownerUserId ?? DBNull.Value),
                new NpgsqlParameter("key", (object?)key ?? DBNull.Value),
                new NpgsqlParameter("name", (object?)name ?? DBNull.Value),
                new NpgsqlParameter("emoji", "\U0001F43E"),
                new NpgsqlParameter("color", "slate"),
                new NpgsqlParameter("sort_order", 50),
                new NpgsqlParameter("archived_at", isArchived ? (object)DateTimeOffset.UtcNow : DBNull.Value));

            return categoryId;
        }
    }
}
