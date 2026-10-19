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
    public class AddPersonalEntryDuplicateTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const int Repetitions = 20;

        [Fact]
        public async Task Add_SameClientRequestIdAndBodyTwice_Answers200BothTimesWithTheSameEntry()
        {
            SignedInUser user = await _app.SignUpAsync();
            EntryInput input = EntryInput.Of(PersonalEntryRequests.BuiltInFoodId);
            Guid clientRequestId = Guid.CreateVersion7();

            HttpResponseMessage first = await PersonalEntryRequests.AddAsync(user.Client, input, clientRequestId);
            HttpResponseMessage second = await PersonalEntryRequests.AddAsync(user.Client, input, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            Assert.Equal((await AuthRequests.ReadJsonAsync(first)).GetRawText(), (await AuthRequests.ReadJsonAsync(second)).GetRawText());
            Assert.Equal(1, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Add_SameClientRequestIdWithOtherFields_AnswersTheFirstEntryUnchanged()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid clientRequestId = Guid.CreateVersion7();
            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, note: "Lunch"), clientRequestId);

            HttpResponseMessage response = await PersonalEntryRequests.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInTransportId, 5_000, "EUR", note: "Taxi"), clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(entryId, body.GetProperty("id").GetGuid());
            Assert.Equal("Lunch", body.GetProperty("note").GetString());
            Assert.Equal("MKD", body.GetProperty("currency").GetString());
            Assert.Equal(1, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Add_ClientRequestIdAlreadyUsedByAnotherUser_Answers400ClientRequestIdUsedAndWritesNothing()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            Guid clientRequestId = Guid.CreateVersion7();
            await PersonalEntrySteps.AddAsync(owner.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId), clientRequestId);
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.AddAsync(other.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId), clientRequestId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_CLIENT_REQUEST_ID_USED);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Add_ClientRequestIdWhoseFirstTryWasRejected_IsFreeForTheCorrectedRequest()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid clientRequestId = Guid.CreateVersion7();
            HttpResponseMessage rejected = await PersonalEntryRequests.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, currency: "USD"), clientRequestId);
            await ProblemResponse.AssertAsync(rejected, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_CURRENCY_INVALID);

            HttpResponseMessage response = await PersonalEntryRequests.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId), clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Add_TwoDifferentClientRequestIds_SaveTwoEntries()
        {
            SignedInUser user = await _app.SignUpAsync();
            EntryInput input = EntryInput.Of(PersonalEntryRequests.BuiltInFoodId);

            await PersonalEntrySteps.AddAsync(user.Client, input);
            await PersonalEntrySteps.AddAsync(user.Client, input);

            Assert.Equal(2, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Add_SameClientRequestIdSentTwiceAtTheSameMoment_Answers200BothTimesWithTheSameEntry()
        {
            SignedInUser user = await _app.SignUpAsync();
            EntryInput input = EntryInput.Of(PersonalEntryRequests.BuiltInFoodId);
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, ScriptedExchangeRateSource.DefaultRate, TimeSpan.Zero);

            for (int repetition = 0; repetition < Repetitions; repetition++)
            {
                Guid clientRequestId = Guid.CreateVersion7();

                HttpResponseMessage[] responses = await Task.WhenAll(
                    PersonalEntryRequests.AddAsync(user.Client, input, clientRequestId),
                    PersonalEntryRequests.AddAsync(user.Client, input, clientRequestId));

                Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK], [.. responses.Select(response => response.StatusCode)]);
                Assert.Equal(
                    (await AuthRequests.ReadJsonAsync(responses[0])).GetProperty("id").GetGuid(),
                    (await AuthRequests.ReadJsonAsync(responses[1])).GetProperty("id").GetGuid());
            }

            Assert.Equal(Repetitions, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Add_SecondRequestWithTheSameClientRequestIdArrivesWhileTheFirstIsStillSaving_Answers200BothTimesWithOneEntry()
        {
            SignedInUser user = await _app.SignUpAsync();
            EntryInput input = EntryInput.Of(PersonalEntryRequests.BuiltInFoodId);
            Guid clientRequestId = Guid.CreateVersion7();
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, ScriptedExchangeRateSource.DefaultRate, TimeSpan.Zero);
            await using InsertGate gate = await InsertGate.CloseAsync(_app, "personal_entries");

            Task<HttpResponseMessage> first = PersonalEntryRequests.AddAsync(user.Client, input, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = PersonalEntryRequests.AddAsync(user.Client, input, clientRequestId);
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
        public async Task Add_SameClientRequestIdFromAnotherUserAtTheSameMoment_AnswersOneEntryAnd400ClientRequestIdUsedForTheOther()
        {
            SignedInUser owner = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            EntryInput input = EntryInput.Of(PersonalEntryRequests.BuiltInFoodId);
            Guid clientRequestId = Guid.CreateVersion7();
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, ScriptedExchangeRateSource.DefaultRate, TimeSpan.Zero);
            await using InsertGate gate = await InsertGate.CloseAsync(_app, "personal_entries");

            Task<HttpResponseMessage> first = PersonalEntryRequests.AddAsync(owner.Client, input, clientRequestId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = PersonalEntryRequests.AddAsync(other.Client, input, clientRequestId);
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
