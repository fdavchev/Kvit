using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.PersonalEntries
{
    public class AddIncomeDuplicateTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const int Repetitions = 20;

        [Fact]
        public async Task Add_SameClientRequestIdAndBodyTwice_Answers200BothTimesWithTheSameIncomeEntry()
        {
            SignedInUser user = await _app.SignUpAsync();
            IncomeEntryInput input = IncomeEntryInput.Of();
            Guid clientRequestId = Guid.CreateVersion7();

            HttpResponseMessage first = await PersonalEntryRequests.AddIncomeAsync(user.Client, input, clientRequestId);
            HttpResponseMessage second = await PersonalEntryRequests.AddIncomeAsync(user.Client, input, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            Assert.Equal((await AuthRequests.ReadJsonAsync(first)).GetRawText(), (await AuthRequests.ReadJsonAsync(second)).GetRawText());
            Assert.Equal(1, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Add_SameClientRequestIdWithOtherFields_AnswersTheFirstIncomeEntryUnchanged()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid clientRequestId = Guid.CreateVersion7();
            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(note: "Salary"), clientRequestId);

            HttpResponseMessage response = await PersonalEntryRequests.AddIncomeAsync(user.Client, IncomeEntryInput.Of(5_000, "EUR", note: "Bonus"), clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(entryId, body.GetProperty("id").GetGuid());
            Assert.Equal("Income", body.GetProperty("kind").GetString());
            Assert.Equal("Salary", body.GetProperty("note").GetString());
            Assert.Equal("MKD", body.GetProperty("currency").GetString());
            Assert.Equal(1, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Add_ClientRequestIdAlreadyUsedByAnotherUser_Answers400ClientRequestIdUsedAndWritesNothing()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            Guid clientRequestId = Guid.CreateVersion7();
            await PersonalEntrySteps.AddIncomeAsync(owner.Client, IncomeEntryInput.Of(), clientRequestId);
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.AddIncomeAsync(other.Client, IncomeEntryInput.Of(), clientRequestId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_CLIENT_REQUEST_ID_USED);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Add_ClientRequestIdWhoseFirstTryWasRejected_IsFreeForTheCorrectedIncome()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid clientRequestId = Guid.CreateVersion7();
            HttpResponseMessage rejected = await PersonalEntryRequests.AddIncomeAsync(user.Client, IncomeEntryInput.Of(currency: "USD"), clientRequestId);
            await ProblemResponse.AssertAsync(rejected, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_CURRENCY_INVALID);

            HttpResponseMessage response = await PersonalEntryRequests.AddIncomeAsync(user.Client, IncomeEntryInput.Of(), clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Add_RepeatAfterTheSavedIncomeWasDeleted_Answers400ClientRequestIdUsedAndSavesNothingNew()
        {
            SignedInUser user = await _app.SignUpAsync();
            IncomeEntryInput input = IncomeEntryInput.Of();
            Guid clientRequestId = Guid.CreateVersion7();
            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, input, clientRequestId);
            HttpResponseMessage deleted = await PersonalEntryRequests.DeleteAsync(user.Client, entryId);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.AddIncomeAsync(user.Client, input, clientRequestId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_CLIENT_REQUEST_ID_USED);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
            Assert.Empty(await PersonalEntrySteps.EntryIdsOfMonthAsync(user.Client));
        }

        [Fact]
        public async Task Add_TwoDifferentClientRequestIds_SaveTwoIncomeEntries()
        {
            SignedInUser user = await _app.SignUpAsync();
            IncomeEntryInput input = IncomeEntryInput.Of();

            await PersonalEntrySteps.AddIncomeAsync(user.Client, input);
            await PersonalEntrySteps.AddIncomeAsync(user.Client, input);

            Assert.Equal(2, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Add_RepeatOfASavedIncomeWhileTheNewestRateIsStale_AnswersTheSavedEntryWithoutAskingTheRateSource()
        {
            SignedInUser user = await _app.SignUpAsync();
            IncomeEntryInput input = IncomeEntryInput.Of();
            Guid clientRequestId = Guid.CreateVersion7();
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, ScriptedExchangeRateSource.DefaultRate, TimeSpan.Zero);
            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, input, clientRequestId);
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, ScriptedExchangeRateSource.DefaultRate, TimeSpan.FromHours(20));
            _app.RateSource.Fail();

            HttpResponseMessage response = await PersonalEntryRequests.AddIncomeAsync(user.Client, input, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(entryId, (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid());
            Assert.Empty(_app.RateSource.RequestedDates);
        }

        [Fact]
        public async Task Add_SameClientRequestIdSentTwiceAtTheSameMoment_Answers200BothTimesWithTheSameIncomeEntry()
        {
            SignedInUser user = await _app.SignUpAsync();
            IncomeEntryInput input = IncomeEntryInput.Of();
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, ScriptedExchangeRateSource.DefaultRate, TimeSpan.Zero);

            for (int repetition = 0; repetition < Repetitions; repetition++)
            {
                Guid clientRequestId = Guid.CreateVersion7();

                HttpResponseMessage[] responses = await Task.WhenAll(
                    PersonalEntryRequests.AddIncomeAsync(user.Client, input, clientRequestId),
                    PersonalEntryRequests.AddIncomeAsync(user.Client, input, clientRequestId));

                Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK], [.. responses.Select(response => response.StatusCode)]);
                Assert.Equal(
                    (await AuthRequests.ReadJsonAsync(responses[0])).GetProperty("id").GetGuid(),
                    (await AuthRequests.ReadJsonAsync(responses[1])).GetProperty("id").GetGuid());
            }

            Assert.Equal(Repetitions, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Add_SecondIncomeRequestWithTheSameClientRequestIdArrivesWhileTheFirstIsStillSaving_Answers200BothTimesWithOneEntry()
        {
            SignedInUser user = await _app.SignUpAsync();
            IncomeEntryInput input = IncomeEntryInput.Of();
            Guid clientRequestId = Guid.CreateVersion7();
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, ScriptedExchangeRateSource.DefaultRate, TimeSpan.Zero);
            await using InsertGate gate = await InsertGate.CloseAsync(_app, "personal_entries");

            Task<HttpResponseMessage> first = PersonalEntryRequests.AddIncomeAsync(user.Client, input, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = PersonalEntryRequests.AddIncomeAsync(user.Client, input, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK], [.. responses.Select(response => response.StatusCode)]);
            Assert.Equal(
                (await AuthRequests.ReadJsonAsync(responses[0])).GetProperty("id").GetGuid(),
                (await AuthRequests.ReadJsonAsync(responses[1])).GetProperty("id").GetGuid());
            Assert.Equal(1, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Add_SameClientRequestIdFromAnotherUserAtTheSameMoment_AnswersOneIncomeEntryAnd400ClientRequestIdUsedForTheOther()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            IncomeEntryInput input = IncomeEntryInput.Of();
            Guid clientRequestId = Guid.CreateVersion7();
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, ScriptedExchangeRateSource.DefaultRate, TimeSpan.Zero);
            await using InsertGate gate = await InsertGate.CloseAsync(_app, "personal_entries");

            Task<HttpResponseMessage> first = PersonalEntryRequests.AddIncomeAsync(owner.Client, input, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = PersonalEntryRequests.AddIncomeAsync(other.Client, input, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
            HttpResponseMessage refused = Assert.Single(responses, response => response.StatusCode != HttpStatusCode.OK);
            await ProblemResponse.AssertAsync(refused, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_CLIENT_REQUEST_ID_USED);
            Assert.Equal(1, await PersonalEntrySteps.CountOfUserAsync(_app, owner.UserId) + await PersonalEntrySteps.CountOfUserAsync(_app, other.UserId));
        }
    }
}
