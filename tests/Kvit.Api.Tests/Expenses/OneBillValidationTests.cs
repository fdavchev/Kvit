using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class OneBillValidationTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(-1)]
        public async Task OneBill_PayerIndexOutOfRange_Answers404MemberNotFoundAndWritesNothing(int payerIndex)
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko", "Petar");

            HttpResponseMessage response = await ExpenseRequests.OneBillAsync(caller.Client, input with { PaidByPersonIndex = payerIndex });

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            await _app.AssertNothingWrittenForAsync(caller);
        }

        [Theory]
        [InlineData(3)]
        [InlineData(-1)]
        public async Task OneBill_ShareIndexOutOfRange_Answers404MemberNotFoundAndWritesNothing(int personIndex)
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko", "Petar");

            HttpResponseMessage response = await ExpenseRequests.OneBillAsync(caller.Client, input with { Shares = [new OneBillShareInput(0, 0), new OneBillShareInput(personIndex, 0)] });

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            await _app.AssertNothingWrittenForAsync(caller);
        }

        [Fact]
        public async Task OneBill_PayerIndexOfTheLastName_IsAccepted()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko", "Petar");

            (Guid groupId, Guid expenseId) = await _app.CreateOneBillAsync(caller, input with { PaidByPersonIndex = 2 });

            Guid petar = await _app.MemberRowIdByNameAsync(groupId, "Petar");
            Assert.Equal(petar.ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "paid_by_member_id"));
        }

        [Fact]
        public async Task OneBill_CurrencyOutsideTheTwoKnownNames_Answers400CurrencyInvalidAndWritesNothing()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, "USD", "Marko");

            await AssertRejectedAsync(caller, input, ResultCodes.EXPENSE_CURRENCY_INVALID);
        }

        [Fact]
        public async Task OneBill_SplitTypeNotSpelledExactly_Answers400SplitTypeInvalidAndWritesNothing()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            await AssertRejectedAsync(caller, input with { SplitType = "equal" }, ResultCodes.EXPENSE_SPLIT_TYPE_INVALID);
        }

        [Fact]
        public async Task OneBill_AmountOfZero_Answers400AmountNotPositiveAndWritesNothing()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(0, ExpenseInputs.Mkd, "Marko");

            await AssertRejectedAsync(caller, input, ResultCodes.EXPENSE_AMOUNT_NOT_POSITIVE);
        }

        [Fact]
        public async Task OneBill_AmountAbove999999999999_Answers400AmountTooLargeAndWritesNothing()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(1_000_000_000_000, ExpenseInputs.Eur, "Marko");

            await AssertRejectedAsync(caller, input, ResultCodes.EXPENSE_AMOUNT_TOO_LARGE);
        }

        [Fact]
        public async Task OneBill_MkdAmountNotAWholeDenar_Answers400NotOnCurrencyStepAndWritesNothing()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(150, ExpenseInputs.Mkd, "Marko");

            await AssertRejectedAsync(caller, input, ResultCodes.MONEY_NOT_ON_CURRENCY_STEP);
        }

        [Fact]
        public async Task OneBill_DateBefore2000_Answers400DateInvalidAndWritesNothing()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            await AssertRejectedAsync(caller, input with { ExpenseDate = "1999-12-31" }, ResultCodes.EXPENSE_DATE_INVALID);
        }

        [Fact]
        public async Task OneBill_NoteLongerThan500Characters_Answers400NoteInvalidAndWritesNothing()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            await AssertRejectedAsync(caller, input with { Note = ExpenseInputs.TextOfLength(501) }, ResultCodes.EXPENSE_NOTE_INVALID);
        }

        [Fact]
        public async Task OneBill_CategoryThatDoesNotExist_Answers400CategoryInvalidAndWritesNothing()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            await AssertRejectedAsync(caller, input with { CategoryId = Guid.CreateVersion7() }, ResultCodes.EXPENSE_CATEGORY_INVALID);
        }

        [Fact]
        public async Task OneBill_ExactThatDoesNotAddUp_Answers400SplitDoesNotAddUpAndWritesNothing()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko") with
            {
                SplitType = "Exact",
                Shares = [new OneBillShareInput(0, 60_000), new OneBillShareInput(1, 30_000)],
            };

            await AssertRejectedAsync(caller, input, ResultCodes.EXPENSE_SPLIT_DOES_NOT_ADD_UP);
        }

        [Fact]
        public async Task OneBill_NoSharesAtAll_Answers400SplitNoParticipantsAndWritesNothing()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko") with { Shares = [] };

            await AssertRejectedAsync(caller, input, ResultCodes.EXPENSE_SPLIT_NO_PARTICIPANTS);
        }

        [Fact]
        public async Task OneBill_BadCurrencyAndATitleOver60Characters_AnswersTheCurrencyCode()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, "USD", "Marko") with { Title = ExpenseInputs.TextOfLength(61) };

            await AssertRejectedAsync(caller, input, ResultCodes.EXPENSE_CURRENCY_INVALID);
        }

        [Fact]
        public async Task OneBill_BadDateAndATitleOver60Characters_AnswersTheDateCode()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko") with { Title = ExpenseInputs.TextOfLength(61), ExpenseDate = "1999-12-31" };

            await AssertRejectedAsync(caller, input, ResultCodes.EXPENSE_DATE_INVALID);
        }

        [Fact]
        public async Task OneBill_ATitleOver60CharactersAndABadNote_AnswersTheTitleCode()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko") with { Title = ExpenseInputs.TextOfLength(61), Note = ExpenseInputs.TextOfLength(501) };

            await AssertRejectedAsync(caller, input, ResultCodes.EXPENSE_TITLE_INVALID);
        }

        private async Task AssertRejectedAsync(SignedInUser caller, OneBillInput input, string expectedErrorCode)
        {
            HttpResponseMessage response = await ExpenseRequests.OneBillAsync(caller.Client, input);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, expectedErrorCode);
            await _app.AssertNothingWrittenForAsync(caller);
        }
    }
}
