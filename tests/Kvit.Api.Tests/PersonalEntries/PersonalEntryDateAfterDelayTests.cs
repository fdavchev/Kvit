using System.Globalization;
using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.PersonalEntries
{
    public class PersonalEntryDateAfterDelayTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string OldestMonthFirstDayOnLastDayOfOctober2026 = "2024-11-01";
        private static readonly DateTimeOffset LastDayOfOctober2026 = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);
        private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

        [Fact]
        public async Task Add_SpendingDatedTomorrowInUtcWhenTypedAndSentADayLater_IsAccepted()
        {
            _app.Clock.SetTo(ExpensesApp.ClockStart);
            SignedInUser user = await _app.SignUpAsync();
            string tomorrowWhenTyped = _app.Clock.UtcToday.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            EntryInput input = EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, entryDate: tomorrowWhenTyped);
            _app.Clock.Advance(OneDay);

            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, input);

            Assert.Equal(tomorrowWhenTyped, await PersonalEntrySteps.ValueAsync(_app, entryId, "entry_date"));
        }

        [Fact]
        public async Task Add_IncomeDatedTomorrowInUtcWhenTypedAndSentADayLater_IsAccepted()
        {
            _app.Clock.SetTo(ExpensesApp.ClockStart);
            SignedInUser user = await _app.SignUpAsync();
            string tomorrowWhenTyped = _app.Clock.UtcToday.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            IncomeEntryInput input = IncomeEntryInput.Of(entryDate: tomorrowWhenTyped);
            _app.Clock.Advance(OneDay);

            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, input);

            Assert.Equal(tomorrowWhenTyped, await PersonalEntrySteps.ValueAsync(_app, entryId, "entry_date"));
        }

        [Fact]
        public async Task Add_SpendingOnTheFirstDayOfTheOldestAllowedMonthSentAfterTheMonthChanged_Answers400DateInvalid()
        {
            _app.Clock.SetTo(LastDayOfOctober2026);
            SignedInUser user = await _app.SignUpAsync();
            EntryInput input = EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, entryDate: OldestMonthFirstDayOnLastDayOfOctober2026);
            await PersonalEntrySteps.AddAsync(user.Client, input);
            _app.Clock.Advance(OneDay);
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.AddAsync(user.Client, input);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_DATE_INVALID);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Add_IncomeOnTheFirstDayOfTheOldestAllowedMonthSentAfterTheMonthChanged_Answers400DateInvalid()
        {
            _app.Clock.SetTo(LastDayOfOctober2026);
            SignedInUser user = await _app.SignUpAsync();
            IncomeEntryInput input = IncomeEntryInput.Of(entryDate: OldestMonthFirstDayOnLastDayOfOctober2026);
            await PersonalEntrySteps.AddIncomeAsync(user.Client, input);
            _app.Clock.Advance(OneDay);
            string before = await PersonalEntrySteps.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.AddIncomeAsync(user.Client, input);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_DATE_INVALID);
            Assert.Equal(before, await PersonalEntrySteps.SnapshotAsync(_app));
        }
    }
}
