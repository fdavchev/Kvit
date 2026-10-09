using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class AddExpenseConcurrentDuplicateTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const int Repetitions = 20;

        [Fact]
        public async Task Add_SameClientRequestIdSentTwiceAtTheSameMoment_Answers200BothTimesWithTheSameExpense()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, ScriptedExchangeRateSource.DefaultRate, TimeSpan.Zero);

            for (int repetition = 0; repetition < Repetitions; repetition++)
            {
                Guid clientRequestId = Guid.CreateVersion7();

                HttpResponseMessage[] responses = await Task.WhenAll(
                    ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input, clientRequestId),
                    ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input, clientRequestId));

                Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK], [.. responses.Select(response => response.StatusCode)]);
                Guid first = (await AuthRequests.ReadJsonAsync(responses[0])).GetProperty("id").GetGuid();
                Guid second = (await AuthRequests.ReadJsonAsync(responses[1])).GetProperty("id").GetGuid();
                Assert.Equal(first, second);
            }

            Assert.Equal(Repetitions, await ExpenseRows.CountExpensesAsync(_app, group.GroupId));
            Assert.Equal(Repetitions, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ExpenseAdded"));
            Assert.Equal(Repetitions, await GroupRows.CountUsageEventsAsync(_app, group.Owner.UserId, "ExpenseAdded"));
        }

        [Fact]
        public async Task Add_SecondRequestWithTheSameClientRequestIdArrivesWhileTheFirstIsStillSaving_Answers200BothTimesWithTheSameExpense()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid clientRequestId = Guid.CreateVersion7();
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, ScriptedExchangeRateSource.DefaultRate, TimeSpan.Zero);
            await using ExpenseInsertGate gate = await ExpenseInsertGate.CloseAsync(_app);

            Task<HttpResponseMessage> first = ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK], [.. responses.Select(response => response.StatusCode)]);
            Guid firstId = (await AuthRequests.ReadJsonAsync(responses[0])).GetProperty("id").GetGuid();
            Guid secondId = (await AuthRequests.ReadJsonAsync(responses[1])).GetProperty("id").GetGuid();
            Assert.Equal(firstId, secondId);
        }

        [Fact]
        public async Task Add_SecondRequestWithTheSameClientRequestIdArrivesWhileTheFirstIsStillSaving_WritesOneExpenseOneSetOfSharesAndOneEvent()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid clientRequestId = Guid.CreateVersion7();
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, ScriptedExchangeRateSource.DefaultRate, TimeSpan.Zero);
            await using ExpenseInsertGate gate = await ExpenseInsertGate.CloseAsync(_app);

            Task<HttpResponseMessage> first = ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Guid expenseId = (await AuthRequests.ReadJsonAsync(responses[0])).GetProperty("id").GetGuid();
            Assert.Equal(1, await ExpenseRows.CountExpensesAsync(_app, group.GroupId));
            Assert.Equal(2, await ExpenseRows.CountSharesOfAsync(_app, expenseId));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ExpenseAdded"));
            Assert.Equal(1, await GroupRows.CountUsageEventsAsync(_app, group.Owner.UserId, "ExpenseAdded"));
        }

        [Fact]
        public async Task Add_SameClientRequestIdFromAnotherGroupAtTheSameMoment_AnswersOneExpenseAnd400ClientRequestIdUsedForTheOther()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseGroup other = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId);
            ExpenseInput inOther = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, other.OwnerRowId, other.OwnerRowId);
            Guid clientRequestId = Guid.CreateVersion7();
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, ScriptedExchangeRateSource.DefaultRate, TimeSpan.Zero);
            await using ExpenseInsertGate gate = await ExpenseInsertGate.CloseAsync(_app);

            Task<HttpResponseMessage> first = ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = ExpenseRequests.AddAsync(other.Owner.Client, other.GroupId, inOther, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            await AssertOneAcceptedAndOneRefusedAsync(responses);
            Assert.Equal(1, await ExpenseRows.CountExpensesAsync(_app, group.GroupId) + await ExpenseRows.CountExpensesAsync(_app, other.GroupId));
            Assert.Equal(1, await GroupRows.CountUsageEventsAsync(_app, group.Owner.UserId, "ExpenseAdded") + await GroupRows.CountUsageEventsAsync(_app, other.Owner.UserId, "ExpenseAdded"));
        }

        [Fact]
        public async Task Add_SameClientRequestIdFromAnotherPersonOfTheSameGroupAtTheSameMoment_AnswersOneExpenseAnd400ClientRequestIdUsedForTheOther()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId);
            Guid clientRequestId = Guid.CreateVersion7();
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, ScriptedExchangeRateSource.DefaultRate, TimeSpan.Zero);
            await using ExpenseInsertGate gate = await ExpenseInsertGate.CloseAsync(_app);

            Task<HttpResponseMessage> first = ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = ExpenseRequests.AddAsync(group.Member.Client, group.GroupId, input, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            await AssertOneAcceptedAndOneRefusedAsync(responses);
            Assert.Equal(1, await ExpenseRows.CountExpensesAsync(_app, group.GroupId));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ExpenseAdded"));
            Assert.Equal(1, await GroupRows.CountUsageEventsAsync(_app, group.Owner.UserId, "ExpenseAdded") + await GroupRows.CountUsageEventsAsync(_app, group.Member.UserId, "ExpenseAdded"));
        }

        [Fact]
        public async Task Add_SameClientRequestIdFromTwoPeopleAtTheSameMomentRepeated_AnswersOne200AndOne400EveryTimeAndSavesOneExpensePerId()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId);
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, ScriptedExchangeRateSource.DefaultRate, TimeSpan.Zero);

            for (int repetition = 0; repetition < Repetitions; repetition++)
            {
                Guid clientRequestId = Guid.CreateVersion7();

                HttpResponseMessage[] responses = await Task.WhenAll(
                    ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input, clientRequestId),
                    ExpenseRequests.AddAsync(group.Member.Client, group.GroupId, input, clientRequestId));

                await AssertOneAcceptedAndOneRefusedAsync(responses);
            }

            Assert.Equal(Repetitions, await ExpenseRows.CountExpensesAsync(_app, group.GroupId));
        }

        private static async Task AssertOneAcceptedAndOneRefusedAsync(HttpResponseMessage[] responses)
        {
            Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
            HttpResponseMessage refused = Assert.Single(responses, response => response.StatusCode != HttpStatusCode.OK);
            await ProblemResponse.AssertAsync(refused, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_CLIENT_REQUEST_ID_USED);
            JsonElement accepted = await AuthRequests.ReadJsonAsync(Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK));
            Assert.NotEqual(Guid.Empty, accepted.GetProperty("id").GetGuid());
        }
    }
}
