using System.Globalization;
using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class DeleteExpenseTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Delete_ByWhoeverAddedIt_Answers204WithNoBody()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));

            HttpResponseMessage response = await ExpenseRequests.DeleteAsync(group.Member.Client, group.GroupId, expenseId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Delete_ByTheOwnerOfAnExpenseAddedByAnotherMember_Answers204()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));

            HttpResponseMessage response = await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(group.Owner.UserId.ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "deleted_by_user_id"));
        }

        [Fact]
        public async Task Delete_ByAMemberWhoDidNotAddItAndIsNotTheOwner_Answers403ExpenseNotAllowedAndChangesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseRequests.DeleteAsync(group.Member.Client, group.GroupId, expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.EXPENSE_NOT_ALLOWED);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Delete_Expense_SetsDeletedAtFromTheClockAndDeletedByAndKeepsTheRowsAndShares()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId));
            string expectedDeletedAt = _app.Clock.GetUtcNow().UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            Assert.Equal(expectedDeletedAt, await ExpenseRows.ExpenseValueAsync(_app, expenseId, "deleted_at AT TIME ZONE 'UTC'"));
            Assert.Equal(group.Owner.UserId.ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "deleted_by_user_id"));
            Assert.Equal(1, await ExpenseRows.CountExpensesAsync(_app, group.GroupId));
            Assert.Equal(2, await ExpenseRows.CountSharesOfAsync(_app, expenseId));
        }

        [Fact]
        public async Task Delete_Expense_WritesOneExpenseDeletedEventWithTheTitleAmountAndCurrency()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(1_550, "EUR", group.OwnerRowId, group.OwnerRowId) with { Title = "Dinner" });

            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ExpenseDeleted"));
            Assert.Equal(group.Owner.UserId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "ExpenseDeleted", "actor_user_id"));
            Assert.Equal(expenseId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "ExpenseDeleted", "expense_id"));
            Assert.Equal("Dinner", await GroupRows.EventValueAsync(_app, group.GroupId, "ExpenseDeleted", "data->>'title'"));
            Assert.Equal("1550", await GroupRows.EventValueAsync(_app, group.GroupId, "ExpenseDeleted", "data->>'amountMinor'"));
            Assert.Equal("EUR", await GroupRows.EventValueAsync(_app, group.GroupId, "ExpenseDeleted", "data->>'currency'"));
        }

        [Fact]
        public async Task Delete_ExpenseWithoutATitle_WritesNoTitleInTheEventData()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));

            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            Assert.Null(await GroupRows.EventValueAsync(_app, group.GroupId, "ExpenseDeleted", "data->>'title'"));
            Assert.Equal("100000", await GroupRows.EventValueAsync(_app, group.GroupId, "ExpenseDeleted", "data->>'amountMinor'"));
        }

        [Fact]
        public async Task Delete_Expense_WritesNoUsageEventBeyondTheAddOne()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));

            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            Assert.Equal(1, await GroupRows.CountUsageEventsAsync(_app, group.Owner.UserId, "ExpenseAdded"));
        }

        [Fact]
        public async Task Delete_Expense_LeavesItOutOfTheListAndTheDetail()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));

            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            JsonElement list = await AuthRequests.ReadJsonAsync(await ExpenseRequests.ListAsync(group.Owner.Client, group.GroupId));
            Assert.Equal(0, list.GetProperty("expenses").GetArrayLength());
            HttpResponseMessage detail = await ExpenseRequests.GetAsync(group.Owner.Client, group.GroupId, expenseId);
            Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
        }

        [Fact]
        public async Task Delete_AlreadyDeletedExpense_Answers404ExpenseNotFoundAndWritesNoSecondEvent()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            HttpResponseMessage response = await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.EXPENSE_NOT_FOUND);
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ExpenseDeleted"));
        }

        [Fact]
        public async Task Delete_ExpenseThatDoesNotExist_Answers404ExpenseNotFound()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            HttpResponseMessage response = await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.EXPENSE_NOT_FOUND);
        }

        [Fact]
        public async Task Delete_ExpenseOfAnotherGroupOfTheSameCaller_Answers404ExpenseNotFoundAndKeepsIt()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid otherGroupId = await _app.CreateGroupAsync(group.Owner);
            Guid ownerRowInOther = await GroupRows.CurrentMemberIdAsync(_app, otherGroupId, group.Owner.UserId);
            Guid expenseInOther = await _app.AddExpenseAsync(group.Owner, otherGroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", ownerRowInOther, ownerRowInOther));

            HttpResponseMessage response = await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseInOther);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.EXPENSE_NOT_FOUND);
            Assert.Null(await ExpenseRows.ExpenseValueAsync(_app, expenseInOther, "deleted_at"));
        }
    }
}
