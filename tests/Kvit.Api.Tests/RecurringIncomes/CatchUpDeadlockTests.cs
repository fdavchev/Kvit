using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Xunit;

namespace Kvit.Api.Tests.RecurringIncomes
{
    public class CatchUpDeadlockTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const int Rounds = 5;
        private const int CallsPerRound = 8;
        private const int WeeklyDueDatesFromJune3 = 20;

        private static readonly DateOnly FirstDueDate = new(2026, 8, 15);
        private static readonly DateOnly LastDueDate = new(2026, 10, 15);

        [Fact]
        public async Task CatchUp_DeadlockedOnceByAnotherWriter_RunsOnceMoreAndSavesEveryDueDate()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 8, 1));
            await using DeadlockPartner partner = await DeadlockPartner.BeginAsync(_app);
            await partner.InsertEntryAsync(user.UserId, recurringIncomeId, LastDueDate);

            Task<HttpResponseMessage> catchUp = RecurringIncomeRequests.CatchUpAsync(user.Client);
            await partner.WaitUntilARequestWaitsForItAsync();
            await partner.InsertEntryAsync(user.UserId, recurringIncomeId, FirstDueDate);
            await partner.RollBackAsync();
            HttpResponseMessage response = await catchUp;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(3, body.GetProperty(CatchUpSteps.CreatedProperty).GetInt32());
            Assert.Equal(["2026-08-15", "2026-09-15", "2026-10-15"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
        }

        [Fact]
        public async Task CatchUp_DeadlockedAgainWhenRunOnceMore_AnswersAnErrorAndSavesNothing()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 8, 1));
            await using DeadlockPartner partner = await DeadlockPartner.BeginAsync(_app);
            await partner.InsertEntryAsync(user.UserId, recurringIncomeId, LastDueDate);

            Task<HttpResponseMessage> catchUp = RecurringIncomeRequests.CatchUpAsync(user.Client);
            await partner.WaitUntilARequestWaitsForItAsync();
            await partner.SetSavepointAsync();
            await partner.InsertEntryAsync(user.UserId, recurringIncomeId, FirstDueDate);
            await partner.WaitUntilARequestWaitsForItAsync();
            await partner.RollBackToSavepointAsync();
            await partner.WaitUntilARequestWaitsForItAsync();
            await partner.InsertEntryAsync(user.UserId, recurringIncomeId, FirstDueDate);
            await partner.RollBackAsync();
            HttpResponseMessage response = await catchUp;

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Empty(await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
            Assert.Equal(0, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task CatchUp_ManyCallsAtTheSameMomentFromTwoDevices_AllAnswer200AndEveryDueDateExistsExactlyOnce()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            SignedInUser secondDevice = await CatchUpSteps.SignInOnASecondDeviceAsync(_app, user);
            HttpClient[] devices = [user.Client, secondDevice.Client];

            for (int round = 0; round < Rounds; round++)
            {
                Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, frequency: "Weekly", dayOfMonth: null, dayOfWeek: 3, startsOn: new DateOnly(2026, 6, 3), title: $"Race {round}");

                HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(0, CallsPerRound).Select(call => RecurringIncomeRequests.CatchUpAsync(devices[call % devices.Length])));

                Assert.Equal(Enumerable.Repeat(HttpStatusCode.OK, CallsPerRound), responses.Select(response => response.StatusCode));
                int sumOfCreated = 0;
                foreach (HttpResponseMessage response in responses)
                {
                    JsonElement body = await AuthRequests.ReadJsonAsync(response);
                    sumOfCreated += body.GetProperty(CatchUpSteps.CreatedProperty).GetInt32();
                }

                List<string> dates = await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId);
                Assert.Equal(WeeklyDueDatesFromJune3, dates.Count);
                Assert.Equal(dates.Count, dates.Distinct().Count());
                Assert.Equal(dates.Count, sumOfCreated);
            }
        }
    }
}
