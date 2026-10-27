using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.BudgetLimits;
using Kvit.Api.Tests.BudgetCurrency;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.PersonalEntries;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.BudgetSummary
{
    public class SummaryAccessAndMonthTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Summary_Anonymous_Answers401()
        {
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage response = await BudgetSummaryRequests.GetAsync(anonymous);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Summary_AnonymousWithABadMonth_Answers401BeforeLookingAtTheMonth()
        {
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage response = await BudgetSummaryRequests.GetAsync(anonymous, "abc");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Summary_ByAGuest_Answers403GuestNotAllowed()
        {
            (ExpenseGroup _, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");

            HttpResponseMessage response = await BudgetSummaryRequests.GetAsync(guest.Client);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
        }

        [Fact]
        public async Task Summary_ByAGuestWithABadMonth_Answers403BeforeLookingAtTheMonth()
        {
            (ExpenseGroup _, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");

            HttpResponseMessage response = await BudgetSummaryRequests.GetAsync(guest.Client, "abc");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GUEST_NOT_ALLOWED);
        }

        [Theory]
        [InlineData("2026-13")]
        [InlineData("2026-00")]
        [InlineData("2026-1")]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("2026-10-01")]
        [InlineData("26-10")]
        [InlineData("2026/10")]
        [InlineData(" 2026-10")]
        public async Task Summary_MonthThatIsNotYearDashMonth_Answers400MonthInvalid(string month)
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await BudgetSummaryRequests.GetAsync(user.Client, month);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_MONTH_INVALID);
        }

        [Fact]
        public async Task Summary_WithoutAMonth_Answers400MonthInvalid()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await BudgetSummaryRequests.GetWithoutMonthAsync(user.Client);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_MONTH_INVALID);
        }

        [Theory]
        [InlineData("9999-12")]
        [InlineData("0001-01")]
        [InlineData("1999-12")]
        public async Task Summary_MonthOutsideTheSupportedRange_Answers400MonthInvalid(string month)
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await BudgetSummaryRequests.GetAsync(user.Client, month);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_MONTH_INVALID);
        }

        [Theory]
        [InlineData("2000-01", "1999-12")]
        [InlineData("9999-11", "9999-10")]
        public async Task Summary_MonthOnTheEdgesOfTheSupportedRange_Answers200WithBothMonthNamesAndNothingToCompare(string month, string previousMonth)
        {
            SignedInUser user = await _app.SignUpAsync();

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(user.Client, month);

            Assert.Equal(month, summary.GetProperty("month").GetString());
            Assert.Equal(previousMonth, summary.GetProperty("previousMonth").GetString());
            BudgetSummarySteps.AssertNothingToCompare(summary);
        }

        [Fact]
        public async Task Summary_EarliestMonth_ComputesThePreviousMonthEvenThoughItIsBeforeTheSupportedRange()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid onTheLastDayOf1999 = await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 7_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            Guid onTheFirstDayOf2000 = await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 3_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await PersonalEntrySteps.SetEntryDateAsync(_app, onTheLastDayOf1999, new DateOnly(1999, 12, 31));
            await PersonalEntrySteps.SetEntryDateAsync(_app, onTheFirstDayOf2000, new DateOnly(2000, 1, 1));

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(user.Client, "2000-01");

            BudgetSummarySteps.AssertCurrencyRow(summary, BudgetCurrencyRequests.Mkd, 3_000, 7_000, 0, 0);
            BudgetSummarySteps.AssertCategories(summary, BudgetCurrencyRequests.Mkd, (BudgetLimitSteps.Food, 3_000, 7_000));
        }

        [Fact]
        public async Task Summary_AnswerHasExactlyTheDocumentedTopLevelFields()
        {
            SignedInUser user = await _app.SignUpAsync();

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(user.Client);

            Assert.Equal(BudgetSummarySteps.SummaryFieldNames, GroupRequests.PropertyNamesOf(summary));
            Assert.Equal(BudgetSummarySteps.ThisMonth, summary.GetProperty("month").GetString());
            Assert.Equal(BudgetSummarySteps.PreviousMonth, summary.GetProperty("previousMonth").GetString());
        }

        [Fact]
        public async Task Summary_MonthWithNothingInEitherMonth_HasAnEmptyCurrenciesListAndNullConvertedTotals()
        {
            SignedInUser user = await _app.SignUpAsync();

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(user.Client);

            Assert.Equal(JsonValueKind.Array, summary.GetProperty("currencies").ValueKind);
            BudgetSummarySteps.AssertNothingToCompare(summary);
            Assert.Equal(JsonValueKind.Null, summary.GetProperty("convertedTotals").ValueKind);
        }

        [Fact]
        public async Task Summary_January_ComparesWithDecemberOfThePreviousYear()
        {
            SignedInUser user = await _app.SignUpAsync();

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(user.Client, "2026-01");

            Assert.Equal("2026-01", summary.GetProperty("month").GetString());
            Assert.Equal("2025-12", summary.GetProperty("previousMonth").GetString());
        }
    }
}
