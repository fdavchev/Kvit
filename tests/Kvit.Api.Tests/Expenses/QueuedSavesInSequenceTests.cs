using System.Net;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class QueuedSavesInSequenceTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string PlainName = "Marko";

        [Fact]
        public async Task Send_TenDifferentQueuedSavesOneAfterAnotherAcrossTwoGroups_AnswersEveryOneWith200AndNoRateLimit()
        {
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, ScriptedExchangeRateSource.DefaultRate, TimeSpan.Zero);
            ExpenseGroup first = await _app.CreateExpenseGroupAsync(PlainName);
            SignedInUser caller = first.Owner;
            Guid secondGroupId = await _app.CreateGroupAsync(caller);
            Guid callerRowInSecond = await GroupRows.CurrentMemberIdAsync(_app, secondGroupId, caller.UserId);
            Guid plainRowInSecond = await GroupRows.InsertMemberAsync(_app, secondGroupId, null, PlainName, caller.UserId);
            ExpenseInput expenseInFirst = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, first.OwnerRowId, first.OwnerRowId, first.MemberRowId);
            ExpenseInput expenseInSecond = ExpenseInputs.Equal(ExpenseInputs.TwelveHundredMkd, ExpenseInputs.Eur, callerRowInSecond, callerRowInSecond, plainRowInSecond);
            OneBillInput billWithMarko = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, PlainName);
            OneBillInput billWithPetar = OneBillInput.EqualAmongEveryone(5_000, ExpenseInputs.Eur, "Petar");
            EntryInput spending = EntryInput.Of(PersonalEntryRequests.BuiltInFoodId);
            IncomeEntryInput income = IncomeEntryInput.Of();
            List<Func<Task<HttpResponseMessage>>> queue =
            [
                () => ExpenseRequests.AddAsync(caller.Client, first.GroupId, expenseInFirst),
                () => ExpenseRequests.AddAsync(caller.Client, secondGroupId, expenseInSecond),
                () => ExpenseRequests.OneBillAsync(caller.Client, billWithMarko),
                () => PersonalEntryRequests.AddAsync(caller.Client, spending),
                () => PersonalEntryRequests.AddIncomeAsync(caller.Client, income),
                () => SettlementRequests.RecordAsync(caller.Client, first.GroupId, first.PlainRowIds[0], first.OwnerRowId, SettlementRequests.DefaultAmountMinor, ExpenseInputs.Mkd),
                () => ExpenseRequests.AddAsync(caller.Client, first.GroupId, expenseInFirst),
                () => SettlementRequests.RecordAsync(caller.Client, secondGroupId, plainRowInSecond, callerRowInSecond, SettlementRequests.DefaultAmountMinor, ExpenseInputs.Mkd),
                () => PersonalEntryRequests.AddAsync(caller.Client, spending),
                () => ExpenseRequests.OneBillAsync(caller.Client, billWithPetar),
            ];
            List<HttpStatusCode> answers = [];

            foreach (Func<Task<HttpResponseMessage>> send in queue)
            {
                HttpResponseMessage response = await send();
                answers.Add(response.StatusCode);
            }

            Assert.Equal(Enumerable.Repeat(HttpStatusCode.OK, queue.Count), answers);
            Assert.Equal(2, await ExpenseRows.CountExpensesAsync(_app, first.GroupId));
            Assert.Equal(1, await ExpenseRows.CountExpensesAsync(_app, secondGroupId));
            Assert.Equal(1, await SettlementRows.CountAsync(_app, first.GroupId));
            Assert.Equal(1, await SettlementRows.CountAsync(_app, secondGroupId));
            Assert.Equal(3, await PersonalEntrySteps.CountOfUserAsync(_app, caller.UserId));
            Assert.Equal(4, await GroupRows.CountGroupsOwnedByAsync(_app, caller.UserId));
        }
    }
}
