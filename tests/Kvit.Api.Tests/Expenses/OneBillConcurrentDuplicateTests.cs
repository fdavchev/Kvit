using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class OneBillConcurrentDuplicateTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const int Repetitions = 20;

        [Fact]
        public async Task OneBill_SameClientRequestIdSentTwiceAtTheSameMoment_Answers200BothTimesWithTheSameGroupIdAndExpenseId()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, ScriptedExchangeRateSource.DefaultRate, TimeSpan.Zero);

            for (int repetition = 0; repetition < Repetitions; repetition++)
            {
                Guid clientRequestId = Guid.CreateVersion7();

                HttpResponseMessage[] responses = await Task.WhenAll(
                    ExpenseRequests.OneBillAsync(caller.Client, input, clientRequestId),
                    ExpenseRequests.OneBillAsync(caller.Client, input, clientRequestId));

                Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK], [.. responses.Select(response => response.StatusCode)]);
                JsonElement first = await AuthRequests.ReadJsonAsync(responses[0]);
                JsonElement second = await AuthRequests.ReadJsonAsync(responses[1]);
                Assert.Equal(first.GetProperty("groupId").GetGuid(), second.GetProperty("groupId").GetGuid());
                Assert.Equal(first.GetProperty("expenseId").GetGuid(), second.GetProperty("expenseId").GetGuid());
            }

            Assert.Equal(Repetitions, await GroupRows.CountGroupsOwnedByAsync(_app, caller.UserId));
            Assert.Equal(Repetitions, await ExpenseRows.CountExpensesCreatedByAsync(_app, caller.UserId));
            Assert.Equal(Repetitions, await GroupRows.CountUsageEventsAsync(_app, caller.UserId, "GroupCreated"));
            Assert.Equal(Repetitions, await GroupRows.CountUsageEventsAsync(_app, caller.UserId, "ExpenseAdded"));
        }

        [Fact]
        public async Task OneBill_SecondRequestWithTheSameClientRequestIdArrivesWhileTheFirstIsStillSaving_Answers200BothTimesWithTheSameGroupIdAndExpenseId()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");
            Guid clientRequestId = Guid.CreateVersion7();
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, ScriptedExchangeRateSource.DefaultRate, TimeSpan.Zero);
            await using ExpenseInsertGate gate = await ExpenseInsertGate.CloseAsync(_app);

            Task<HttpResponseMessage> first = ExpenseRequests.OneBillAsync(caller.Client, input, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = ExpenseRequests.OneBillAsync(caller.Client, input, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK], [.. responses.Select(response => response.StatusCode)]);
            JsonElement firstBody = await AuthRequests.ReadJsonAsync(responses[0]);
            JsonElement secondBody = await AuthRequests.ReadJsonAsync(responses[1]);
            Assert.Equal(firstBody.GetProperty("groupId").GetGuid(), secondBody.GetProperty("groupId").GetGuid());
            Assert.Equal(firstBody.GetProperty("expenseId").GetGuid(), secondBody.GetProperty("expenseId").GetGuid());
        }

        [Fact]
        public async Task OneBill_SecondRequestWithTheSameClientRequestIdArrivesWhileTheFirstIsStillSaving_WritesOneGroupOneSetOfMembersAndOneSetOfEvents()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");
            Guid clientRequestId = Guid.CreateVersion7();
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, ScriptedExchangeRateSource.DefaultRate, TimeSpan.Zero);
            await using ExpenseInsertGate gate = await ExpenseInsertGate.CloseAsync(_app);

            Task<HttpResponseMessage> first = ExpenseRequests.OneBillAsync(caller.Client, input, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = ExpenseRequests.OneBillAsync(caller.Client, input, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            JsonElement body = await AuthRequests.ReadJsonAsync(responses[0]);
            Guid groupId = body.GetProperty("groupId").GetGuid();
            Guid expenseId = body.GetProperty("expenseId").GetGuid();
            Assert.Equal(1, await GroupRows.CountGroupsOwnedByAsync(_app, caller.UserId));
            Assert.Equal(1, await ExpenseRows.CountExpensesCreatedByAsync(_app, caller.UserId));
            Assert.Equal(2, await GroupRows.CountMembersAsync(_app, groupId));
            Assert.Equal(2, await ExpenseRows.CountSharesOfAsync(_app, expenseId));
            Assert.Equal(1, await GroupRows.CountUsageEventsAsync(_app, caller.UserId, "GroupCreated"));
            Assert.Equal(1, await GroupRows.CountUsageEventsAsync(_app, caller.UserId, "ExpenseAdded"));
            Assert.Equal(3, await ExpenseRows.CountEventsByActorAsync(_app, caller.UserId));
        }

        [Fact]
        public async Task OneBill_SameClientRequestIdFromAnotherPersonAtTheSameMoment_AnswersOneBillAnd400ClientRequestIdUsedForTheOther()
        {
            SignedInUser first = await _app.SignUpAsync();
            SignedInUser second = await _app.SignUpAsync("Bojan");
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");
            Guid clientRequestId = Guid.CreateVersion7();
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, ScriptedExchangeRateSource.DefaultRate, TimeSpan.Zero);
            await using ExpenseInsertGate gate = await ExpenseInsertGate.CloseAsync(_app);

            Task<HttpResponseMessage> firstRequest = ExpenseRequests.OneBillAsync(first.Client, input, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> secondRequest = ExpenseRequests.OneBillAsync(second.Client, input, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(firstRequest, secondRequest);

            SignedInUser[] callers = [first, second];
            await AssertOneAcceptedAndOneRefusedAsync(responses, callers);
        }

        [Fact]
        public async Task OneBill_SameClientRequestIdFromTwoPeopleAtTheSameMomentRepeated_AnswersOne200AndOne400EveryTimeAndSavesOneBillPerId()
        {
            SignedInUser first = await _app.SignUpAsync();
            SignedInUser second = await _app.SignUpAsync("Bojan");
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, ScriptedExchangeRateSource.DefaultRate, TimeSpan.Zero);

            for (int repetition = 0; repetition < Repetitions; repetition++)
            {
                Guid clientRequestId = Guid.CreateVersion7();

                HttpResponseMessage[] responses = await Task.WhenAll(
                    ExpenseRequests.OneBillAsync(first.Client, input, clientRequestId),
                    ExpenseRequests.OneBillAsync(second.Client, input, clientRequestId));

                Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
                HttpResponseMessage refused = Assert.Single(responses, response => response.StatusCode != HttpStatusCode.OK);
                await ProblemResponse.AssertAsync(refused, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_CLIENT_REQUEST_ID_USED);
            }

            Assert.Equal(Repetitions, await ExpenseRows.CountExpensesCreatedByAsync(_app, first.UserId) + await ExpenseRows.CountExpensesCreatedByAsync(_app, second.UserId));
            Assert.Equal(Repetitions, await GroupRows.CountGroupsOwnedByAsync(_app, first.UserId) + await GroupRows.CountGroupsOwnedByAsync(_app, second.UserId));
        }

        private async Task AssertOneAcceptedAndOneRefusedAsync(HttpResponseMessage[] responses, SignedInUser[] callers)
        {
            Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
            int refusedIndex = Array.FindIndex(responses, response => response.StatusCode != HttpStatusCode.OK);
            await ProblemResponse.AssertAsync(responses[refusedIndex], HttpStatusCode.BadRequest, ResultCodes.EXPENSE_CLIENT_REQUEST_ID_USED);
            await _app.AssertNothingWrittenForAsync(callers[refusedIndex]);
            SignedInUser accepted = callers[1 - refusedIndex];
            Assert.Equal(1, await GroupRows.CountGroupsOwnedByAsync(_app, accepted.UserId));
            Assert.Equal(1, await ExpenseRows.CountExpensesCreatedByAsync(_app, accepted.UserId));
        }
    }
}
