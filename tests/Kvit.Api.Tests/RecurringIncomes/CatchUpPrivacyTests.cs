using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Xunit;

namespace Kvit.Api.Tests.RecurringIncomes
{
    public class CatchUpPrivacyTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task CatchUp_UserWithoutIncomes_NeverCreatesEntriesForSomeoneElsesIncome()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser owner = await CatchUpSteps.SignUpAsync(_app);
            SignedInUser other = await CatchUpSteps.SignUpAsync(_app, "Bojan");
            Guid recurringIncomeId = await RecurringIncomeRows.InsertAsync(_app, owner.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 8, 1));

            int created = await CatchUpSteps.CatchUpAsync(other.Client);

            Assert.Equal(0, created);
            Assert.Empty(await CatchUpSteps.EntryDatesAsync(_app, recurringIncomeId));
            Assert.Equal(0, await PersonalEntrySteps.CountOfUserAsync(_app, owner.UserId));
            Assert.Equal(0, await PersonalEntrySteps.CountOfUserAsync(_app, other.UserId));
        }

        [Fact]
        public async Task CatchUp_ByAnotherUser_LeavesTheOwnersEntriesExactlyAsTheyWere()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser owner = await CatchUpSteps.SignUpAsync(_app);
            SignedInUser other = await CatchUpSteps.SignUpAsync(_app, "Bojan");
            Guid ownersIncome = await RecurringIncomeRows.InsertAsync(_app, owner.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 8, 1));
            await CatchUpSteps.CatchUpAsync(owner.Client);
            await RecurringIncomeRows.InsertAsync(_app, other.UserId, dayOfMonth: 1, startsOn: new DateOnly(2026, 10, 1));
            string ownersEntriesBefore = await CatchUpSteps.EntriesSnapshotOfUserAsync(_app, owner.UserId);
            string incomesBefore = await RecurringIncomeRows.SnapshotAsync(_app);

            int created = await CatchUpSteps.CatchUpAsync(other.Client);

            Assert.Equal(1, created);
            Assert.Equal(ownersEntriesBefore, await CatchUpSteps.EntriesSnapshotOfUserAsync(_app, owner.UserId));
            Assert.Equal(["2026-08-15", "2026-09-15", "2026-10-15"], await CatchUpSteps.EntryDatesAsync(_app, ownersIncome));
            Assert.Equal(incomesBefore, await RecurringIncomeRows.SnapshotAsync(_app));
        }

        [Fact]
        public async Task CatchUp_TwoUsersWithIncomes_EachGetsOnlyEntriesForTheirOwnIncomes()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser first = await CatchUpSteps.SignUpAsync(_app);
            SignedInUser second = await CatchUpSteps.SignUpAsync(_app, "Bojan");
            Guid firstIncome = await RecurringIncomeRows.InsertAsync(_app, first.UserId, amountMinor: 4_500_000, dayOfMonth: 15, startsOn: new DateOnly(2026, 9, 1));
            Guid secondIncome = await RecurringIncomeRows.InsertAsync(_app, second.UserId, amountMinor: 20_000, currency: "EUR", dayOfMonth: 1, startsOn: new DateOnly(2026, 9, 1));

            int createdForSecond = await CatchUpSteps.CatchUpAsync(second.Client);

            Assert.Equal(2, createdForSecond);
            Assert.Equal(["2026-09-01", "2026-10-01"], await CatchUpSteps.EntryDatesAsync(_app, secondIncome));
            Assert.Equal(["2026-09-01", "2026-10-01"], await CatchUpSteps.EntryDatesOfUserAsync(_app, second.UserId));
            Assert.Empty(await CatchUpSteps.EntryDatesAsync(_app, firstIncome));
            Assert.Empty(await CatchUpSteps.EntryDatesOfUserAsync(_app, first.UserId));
            int createdForFirst = await CatchUpSteps.CatchUpAsync(first.Client);
            Assert.Equal(2, createdForFirst);
            Assert.Equal(["2026-09-15", "2026-10-15"], await CatchUpSteps.EntryDatesOfUserAsync(_app, first.UserId));
            Assert.Equal(["2026-09-01", "2026-10-01"], await CatchUpSteps.EntryDatesOfUserAsync(_app, second.UserId));
        }

        [Fact]
        public async Task CatchUp_ByAnotherUserWhoseOwnIncomeIsDue_DoesNotShowUpInTheOwnersBudgetMonth()
        {
            CatchUpSteps.SetNow(_app, CatchUpSteps.NoonOnOctober20);
            SignedInUser owner = await CatchUpSteps.SignUpAsync(_app);
            SignedInUser other = await CatchUpSteps.SignUpAsync(_app, "Bojan");
            await RecurringIncomeRows.InsertAsync(_app, other.UserId, dayOfMonth: 15, startsOn: new DateOnly(2026, 10, 1));
            await CatchUpSteps.CatchUpAsync(other.Client);

            Assert.Empty(await PersonalEntrySteps.EntryIdsOfMonthAsync(owner.Client));
            Assert.Single(await PersonalEntrySteps.EntryIdsOfMonthAsync(other.Client));
        }
    }
}
