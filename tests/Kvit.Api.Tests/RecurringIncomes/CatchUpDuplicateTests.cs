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
    public class CatchUpDuplicateTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task CatchUp_TwoDevicesAtTheSameMoment_BothAnswer200AndEveryDueDateExistsExactlyOnce()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            SignedInUser secondDevice = await CatchUpSteps.SignInOnASecondDeviceAsync(_app, user);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 8, 1));
            await using InsertGate gate = await InsertGate.CloseAsync(_app, "personal_entries");

            Task<HttpResponseMessage> first = RecurringIncomeRequests.CatchUpAsync(user.Client);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = RecurringIncomeRequests.CatchUpAsync(secondDevice.Client);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK], [.. responses.Select(response => response.StatusCode)]);
            Assert.Equal(["2026-08-15", "2026-09-15", "2026-10-15"], await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
            int sumOfCreated = 0;
            foreach (HttpResponseMessage response in responses)
            {
                JsonElement body = await AuthRequests.ReadJsonAsync(response);
                sumOfCreated += body.GetProperty(CatchUpSteps.CreatedProperty).GetInt32();
            }

            Assert.Equal(3, sumOfCreated);
        }

        [Fact]
        public async Task CatchUp_AfterTheSameMomentRace_AThirdCallCreatesNothing()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            SignedInUser secondDevice = await CatchUpSteps.SignInOnASecondDeviceAsync(_app, user);
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 8, 1));
            await using (InsertGate gate = await InsertGate.CloseAsync(_app, "personal_entries"))
            {
                Task<HttpResponseMessage> first = RecurringIncomeRequests.CatchUpAsync(user.Client);
                await gate.WaitUntilRequestsAreHeldAsync(1);
                Task<HttpResponseMessage> second = RecurringIncomeRequests.CatchUpAsync(secondDevice.Client);
                await gate.WaitUntilRequestsAreHeldAsync(2);
                await gate.OpenAsync();
                await Task.WhenAll(first, second);
            }

            int created = await CatchUpSteps.CatchUpAsync(user.Client);

            Assert.Equal(0, created);
            Assert.Equal(3, (await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId)).Count);
            Assert.Equal(3, await PersonalEntrySteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task CatchUp_TwoDevicesAtTheSameMomentWithSeveralIncomes_EachIncomeKeepsExactlyOneEntryPerDueDate()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            SignedInUser secondDevice = await CatchUpSteps.SignInOnASecondDeviceAsync(_app, user);
            Guid monthly = await RecurringIncomeRows.InsertAsync(_app, user.UserId, dayOfMonth: 1, startsOn: new DateOnly(2026, 9, 1));
            Guid weekly = await RecurringIncomeRows.InsertAsync(_app, user.UserId, frequency: "Weekly", dayOfMonth: null, dayOfWeek: 3, startsOn: new DateOnly(2026, 10, 1));
            await using InsertGate gate = await InsertGate.CloseAsync(_app, "personal_entries");

            Task<HttpResponseMessage> first = RecurringIncomeRequests.CatchUpAsync(user.Client);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = RecurringIncomeRequests.CatchUpAsync(secondDevice.Client);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK], [.. responses.Select(response => response.StatusCode)]);
            Assert.Equal(["2026-09-01", "2026-10-01"], await CatchUpSteps.EntryDatesAsync(_app, monthly));
            Assert.Equal(["2026-10-07", "2026-10-14"], await CatchUpSteps.EntryDatesAsync(_app, weekly));
        }
    }
}
