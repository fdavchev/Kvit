using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.RecurringIncomes
{
    public class CreateRecurringIncomeDuplicateTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Create_SameClientRequestIdAndBodyTwice_Answers200BothTimesWithTheSameRow()
        {
            SignedInUser user = await _app.SignUpAsync();
            RecurringIncomeBody body = RecurringIncomeBody.Monthly();
            Guid clientRequestId = Guid.CreateVersion7();

            HttpResponseMessage first = await RecurringIncomeRequests.CreateAsync(user.Client, body, clientRequestId);
            HttpResponseMessage second = await RecurringIncomeRequests.CreateAsync(user.Client, body, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            Assert.Equal((await AuthRequests.ReadJsonAsync(first)).GetRawText(), (await AuthRequests.ReadJsonAsync(second)).GetRawText());
            Assert.Equal(1, await RecurringIncomeRows.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Create_SameClientRequestIdWithOtherFields_AnswersTheFirstRowUnchanged()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid clientRequestId = Guid.CreateVersion7();
            Guid recurringIncomeId = await RecurringIncomeSteps.CreateAsync(user.Client, RecurringIncomeBody.Monthly(title: "Salary"), clientRequestId);

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Weekly(5_000, "EUR", "Bonus"), clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(recurringIncomeId, body.GetProperty("id").GetGuid());
            Assert.Equal("Salary", body.GetProperty("title").GetString());
            Assert.Equal("MKD", body.GetProperty("currency").GetString());
            Assert.Equal("Monthly", body.GetProperty("frequency").GetString());
            Assert.Equal(1, await RecurringIncomeRows.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Create_ClientRequestIdAlreadyUsedByAnotherUser_Answers400ClientRequestIdUsedAndWritesNothing()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            Guid clientRequestId = Guid.CreateVersion7();
            await RecurringIncomeSteps.CreateAsync(owner.Client, RecurringIncomeBody.Monthly(), clientRequestId);
            string before = await RecurringIncomeRows.SnapshotAsync(_app);

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(other.Client, RecurringIncomeBody.Monthly(), clientRequestId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_CLIENT_REQUEST_ID_USED);
            Assert.Equal(before, await RecurringIncomeRows.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Create_ClientRequestIdWhoseFirstTryWasRejected_IsFreeForTheCorrectedIncome()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid clientRequestId = Guid.CreateVersion7();
            HttpResponseMessage rejected = await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Monthly(currency: "USD"), clientRequestId);
            await ProblemResponse.AssertAsync(rejected, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_CURRENCY_INVALID);

            HttpResponseMessage response = await RecurringIncomeRequests.CreateAsync(user.Client, RecurringIncomeBody.Monthly(), clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, await RecurringIncomeRows.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Create_TwoDifferentClientRequestIds_SaveTwoRows()
        {
            SignedInUser user = await _app.SignUpAsync();
            RecurringIncomeBody body = RecurringIncomeBody.Monthly();

            await RecurringIncomeSteps.CreateAsync(user.Client, body);
            await RecurringIncomeSteps.CreateAsync(user.Client, body);

            Assert.Equal(2, await RecurringIncomeRows.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Create_SecondRequestWithTheSameClientRequestIdArrivesWhileTheFirstIsStillSaving_Answers200BothTimesWithOneRow()
        {
            SignedInUser user = await _app.SignUpAsync();
            RecurringIncomeBody body = RecurringIncomeBody.Monthly();
            Guid clientRequestId = Guid.CreateVersion7();
            await using InsertGate gate = await InsertGate.CloseAsync(_app, "recurring_incomes");

            Task<HttpResponseMessage> first = RecurringIncomeRequests.CreateAsync(user.Client, body, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = RecurringIncomeRequests.CreateAsync(user.Client, body, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK], [.. responses.Select(response => response.StatusCode)]);
            Assert.Equal(
                (await AuthRequests.ReadJsonAsync(responses[0])).GetProperty("id").GetGuid(),
                (await AuthRequests.ReadJsonAsync(responses[1])).GetProperty("id").GetGuid());
            Assert.Equal(1, await RecurringIncomeRows.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Create_SameClientRequestIdFromAnotherUserAtTheSameMoment_AnswersOneRowAnd400ClientRequestIdUsedForTheOther()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            RecurringIncomeBody body = RecurringIncomeBody.Monthly();
            Guid clientRequestId = Guid.CreateVersion7();
            await using InsertGate gate = await InsertGate.CloseAsync(_app, "recurring_incomes");

            Task<HttpResponseMessage> first = RecurringIncomeRequests.CreateAsync(owner.Client, body, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = RecurringIncomeRequests.CreateAsync(other.Client, body, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
            HttpResponseMessage refused = Assert.Single(responses, response => response.StatusCode != HttpStatusCode.OK);
            await ProblemResponse.AssertAsync(refused, HttpStatusCode.BadRequest, ResultCodes.RECURRING_INCOME_CLIENT_REQUEST_ID_USED);
            Assert.Equal(1, await RecurringIncomeRows.CountOfUserAsync(_app, owner.UserId) + await RecurringIncomeRows.CountOfUserAsync(_app, other.UserId));
        }
    }
}
