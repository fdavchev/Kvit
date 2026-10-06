using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class AddExpenseCheckOrderTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string TooEarly = "1999-12-31";

        [Fact]
        public async Task Add_BadCurrencyAndBadSplitType_AnswersTheCurrencyCode()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ValidInput(group) with { Currency = "USD", SplitType = "Unknown" };

            await AssertAnswersAsync(group, input, ResultCodes.EXPENSE_CURRENCY_INVALID);
        }

        [Fact]
        public async Task Add_BadSplitTypeAndBadAmount_AnswersTheSplitTypeCode()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ValidInput(group) with { SplitType = "Unknown", AmountMinor = 0 };

            await AssertAnswersAsync(group, input, ResultCodes.EXPENSE_SPLIT_TYPE_INVALID);
        }

        [Fact]
        public async Task Add_BadAmountAndBadDate_AnswersTheAmountCode()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ValidInput(group) with { AmountMinor = 0, ExpenseDate = TooEarly };

            await AssertAnswersAsync(group, input, ResultCodes.EXPENSE_AMOUNT_NOT_POSITIVE);
        }

        [Fact]
        public async Task Add_AmountOffTheStepAndBadDate_AnswersTheStepCode()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ValidInput(group) with { AmountMinor = 150, ExpenseDate = TooEarly };

            await AssertAnswersAsync(group, input, ResultCodes.MONEY_NOT_ON_CURRENCY_STEP);
        }

        [Fact]
        public async Task Add_BadDateAndBadTitle_AnswersTheDateCode()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ValidInput(group) with { ExpenseDate = TooEarly, Title = ExpenseInputs.TextOfLength(81) };

            await AssertAnswersAsync(group, input, ResultCodes.EXPENSE_DATE_INVALID);
        }

        [Fact]
        public async Task Add_BadTitleAndBadNote_AnswersTheTitleCode()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ValidInput(group) with { Title = ExpenseInputs.TextOfLength(81), Note = ExpenseInputs.TextOfLength(501) };

            await AssertAnswersAsync(group, input, ResultCodes.EXPENSE_TITLE_INVALID);
        }

        [Fact]
        public async Task Add_BadNoteAndBadCategory_AnswersTheNoteCode()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ValidInput(group) with { Note = ExpenseInputs.TextOfLength(501), CategoryId = Guid.CreateVersion7() };

            await AssertAnswersAsync(group, input, ResultCodes.EXPENSE_NOTE_INVALID);
        }

        [Fact]
        public async Task Add_BadCategoryAndBadSplit_AnswersTheCategoryCode()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ValidInput(group) with { CategoryId = Guid.CreateVersion7(), Shares = [] };

            await AssertAnswersAsync(group, input, ResultCodes.EXPENSE_CATEGORY_INVALID);
        }

        [Fact]
        public async Task Add_EverythingWrong_AnswersTheCurrencyCode()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = new(
                ExpenseInputs.TextOfLength(81),
                ExpenseInputs.TextOfLength(501),
                0,
                "USD",
                TooEarly,
                Guid.CreateVersion7(),
                group.OwnerRowId,
                "Unknown",
                []);

            await AssertAnswersAsync(group, input, ResultCodes.EXPENSE_CURRENCY_INVALID);
        }

        [Fact]
        public async Task Add_ByANonMemberWithAnInvalidBody_Answers404GroupNotFoundBeforeLookingAtTheInput()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");
            ExpenseInput input = ValidInput(group) with { Currency = "USD" };

            HttpResponseMessage response = await ExpenseRequests.AddAsync(stranger.Client, group.GroupId, input);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
        }

        private static ExpenseInput ValidInput(ExpenseGroup group)
        {
            return ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
        }

        private static async Task AssertAnswersAsync(ExpenseGroup group, ExpenseInput input, string expectedErrorCode)
        {
            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, expectedErrorCode);
        }
    }
}
