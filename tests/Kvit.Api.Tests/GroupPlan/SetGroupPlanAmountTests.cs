using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.GroupPlan
{
    public class SetGroupPlanAmountTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string StoredBefore = "200000:MKD";

        [Theory]
        [InlineData("MKD", 100)]
        [InlineData("MKD", 100_000)]
        [InlineData("MKD", GroupPlanRequests.LargestMkdAmount)]
        [InlineData("EUR", 1)]
        [InlineData("EUR", 150)]
        [InlineData("EUR", GroupPlanRequests.LargestEurAmount)]
        public async Task Put_SmallestAndLargestAmountsOfACurrency_AreAccepted(string currency, long amountMinor)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            HttpResponseMessage response = await GroupPlanRequests.SetAsync(group.Owner.Client, group.GroupId, amountMinor, currency);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(GroupPlanRows.TextOf(amountMinor, currency), await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        [Theory]
        [InlineData("MKD", 0)]
        [InlineData("MKD", -100)]
        [InlineData("MKD", -1)]
        [InlineData("MKD", long.MinValue)]
        [InlineData("MKD", 1)]
        [InlineData("MKD", 99)]
        [InlineData("MKD", 101)]
        [InlineData("MKD", 150)]
        [InlineData("MKD", 150_050)]
        [InlineData("MKD", GroupPlanRequests.LargestEurAmount)]
        [InlineData("MKD", GroupPlanRequests.AboveTheBound)]
        [InlineData("MKD", long.MaxValue)]
        [InlineData("EUR", 0)]
        [InlineData("EUR", -1)]
        [InlineData("EUR", long.MinValue)]
        [InlineData("EUR", GroupPlanRequests.AboveTheBound)]
        [InlineData("EUR", long.MaxValue)]
        public async Task Put_AmountThatIsNotAPositiveWholeStepWithinTheBound_Answers400WithThePlanAmountCodeAndKeepsThePlan(string currency, long amountMinor)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

            HttpResponseMessage response = await GroupPlanRequests.SetAsync(group.Owner.Client, group.GroupId, amountMinor, currency);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, GroupPlanRequests.AmountInvalidCode);
            Assert.Equal(StoredBefore, await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, GroupPlanSteps.SetEventType));
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{\"currency\":\"MKD\"}")]
        [InlineData("{\"amount\":100000,\"currency\":\"MKD\"}")]
        [InlineData("{\"amountMinor\":null,\"currency\":\"MKD\"}")]
        [InlineData("{\"amountMinor\":\"abc\",\"currency\":\"MKD\"}")]
        [InlineData("{\"amountMinor\":\"100000\",\"currency\":\"MKD\"}")]
        [InlineData("{\"amountMinor\":true,\"currency\":\"MKD\"}")]
        [InlineData("{\"amountMinor\":[],\"currency\":\"MKD\"}")]
        [InlineData("{\"amountMinor\":150.5,\"currency\":\"MKD\"}")]
        [InlineData("{\"amountMinor\":100000.5,\"currency\":\"EUR\"}")]
        [InlineData("{\"amountMinor\":99999999999999999999,\"currency\":\"MKD\"}")]
        [InlineData("{\"amountMinor\":-99999999999999999999,\"currency\":\"EUR\"}")]
        public async Task Put_BodyWithoutAUsableWholeNumber_Answers400WithThePlanAmountCodeAndKeepsThePlan(string json)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

            HttpResponseMessage response = await GroupPlanRequests.SetWithJsonTextAsync(group.Owner.Client, group.GroupId, json);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, GroupPlanRequests.AmountInvalidCode);
            Assert.Equal(StoredBefore, await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        [Theory]
        [InlineData("not json")]
        [InlineData("{\"amountMinor\":100000,")]
        [InlineData("")]
        [InlineData("null")]
        [InlineData("[]")]
        [InlineData("\"100000\"")]
        public async Task Put_BrokenOrEmptyBodyOrOneThatIsNotAnObject_Answers400AndKeepsThePlan(string json)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

            HttpResponseMessage response = await GroupPlanRequests.SetWithJsonTextAsync(group.Owner.Client, group.GroupId, json);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(StoredBefore, await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        [Theory]
        [InlineData("{\"amountMinor\":100000,\"currency\":\"USD\"}")]
        [InlineData("{\"amountMinor\":100000,\"currency\":\"\"}")]
        [InlineData("{\"amountMinor\":100000,\"currency\":\"   \"}")]
        [InlineData("{\"amountMinor\":100000,\"currency\":\"mkd\"}")]
        [InlineData("{\"amountMinor\":100000,\"currency\":\"eur\"}")]
        [InlineData("{\"amountMinor\":100000,\"currency\":\"Eur\"}")]
        [InlineData("{\"amountMinor\":100000,\"currency\":\"MKD \"}")]
        [InlineData("{\"amountMinor\":100000,\"currency\":\" EUR\"}")]
        [InlineData("{\"amountMinor\":100000,\"currency\":null}")]
        [InlineData("{\"amountMinor\":100000}")]
        public async Task Put_CurrencyThatIsNotExactlyMkdOrEur_Answers400GroupCurrencyInvalidAndKeepsThePlan(string json)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

            HttpResponseMessage response = await GroupPlanRequests.SetWithJsonTextAsync(group.Owner.Client, group.GroupId, json);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_CURRENCY_INVALID);
            Assert.Equal(StoredBefore, await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, GroupPlanSteps.SetEventType));
        }

        [Fact]
        public async Task Put_BadAmountAndBadCurrency_AnswersThePlanAmountCodeBecauseTheAmountIsCheckedFirst()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            HttpResponseMessage response = await GroupPlanRequests.SetAsync(group.Owner.Client, group.GroupId, 0, "USD");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, GroupPlanRequests.AmountInvalidCode);
            Assert.Equal(GroupPlanRows.NoPlan, await GroupPlanRows.PlanOfAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Put_NoUsableAmountAndBadCurrency_AnswersThePlanAmountCodeBecauseTheAmountIsCheckedFirst()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            HttpResponseMessage response = await GroupPlanRequests.SetWithJsonTextAsync(group.Owner.Client, group.GroupId, "{\"amountMinor\":\"abc\",\"currency\":\"USD\"}");

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, GroupPlanRequests.AmountInvalidCode);
        }

        [Fact]
        public async Task Put_ARefusedAmount_AddsNoActivityOrUsageEvent()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            int eventsBefore = await GroupRows.CountEventsAsync(_app, group.GroupId);
            string usageBefore = await GroupPlanSteps.UsageSnapshotAsync(_app, group.Owner, group.Member);

            HttpResponseMessage badAmount = await GroupPlanRequests.SetAsync(group.Owner.Client, group.GroupId, 150, GroupPlanRequests.Mkd);
            HttpResponseMessage badCurrency = await GroupPlanRequests.SetAsync(group.Owner.Client, group.GroupId, 100, "USD");

            Assert.Equal(HttpStatusCode.BadRequest, badAmount.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, badCurrency.StatusCode);
            Assert.Equal(eventsBefore, await GroupRows.CountEventsAsync(_app, group.GroupId));
            Assert.Equal(usageBefore, await GroupPlanSteps.UsageSnapshotAsync(_app, group.Owner, group.Member));
        }
    }
}
