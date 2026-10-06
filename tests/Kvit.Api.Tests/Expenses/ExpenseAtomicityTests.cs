using System.Net;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class ExpenseAtomicityTests(ExpensesApp _app) : IClassFixture<ExpensesApp>, IAsyncLifetime
    {
        private const string BlockedMessage = "expense_shares are blocked by the test";

        public ValueTask InitializeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            await ExpenseRows.AllowAllShareChangesAsync(_app);
        }

        [Fact]
        public async Task OneBill_WhenTheLastWriteFails_WritesNothingNotEvenTheGroup()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko", "Petar");
            await ExpenseRows.BlockAllShareChangesAsync(_app);

            HttpResponseMessage response = await ExpenseRequests.OneBillAsync(caller.Client, input);

            await AssertFailedBecauseTheSharesAreBlockedAsync(response);
            await _app.AssertNothingWrittenForAsync(caller);
        }

        [Fact]
        public async Task Add_WhenTheSharesCannotBeSaved_WritesNoExpenseNoEventAndNoUsageEvent()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);
            await ExpenseRows.BlockAllShareChangesAsync(_app);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input);

            await AssertFailedBecauseTheSharesAreBlockedAsync(response);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(0, await GroupRows.CountUsageEventsAsync(_app, group.Owner.UserId, "ExpenseAdded"));
        }

        [Fact]
        public async Task Edit_WhenTheSharesCannotBeSaved_ChangesNothingNotEvenTheExpenseRow()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);
            await ExpenseRows.BlockAllShareChangesAsync(_app);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { AmountMinor = 200_000, Title = "Bigger" });

            await AssertFailedBecauseTheSharesAreBlockedAsync(response);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }

        private static async Task AssertFailedBecauseTheSharesAreBlockedAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Contains(BlockedMessage, body);
        }
    }
}
