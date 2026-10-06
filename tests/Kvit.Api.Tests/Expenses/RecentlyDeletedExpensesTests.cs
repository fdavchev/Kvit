using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class RecentlyDeletedExpensesTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly TimeSpan FiveDays = TimeSpan.FromDays(5);
        private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        [Fact]
        public async Task ListDeleted_GroupWithoutDeletedExpenses_AnswersOnlyAnEmptyExpensesArray()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            HttpResponseMessage response = await ExpenseRequests.ListDeletedAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(["expenses"], GroupRequests.PropertyNamesOf(body));
            Assert.Equal(0, body.GetProperty("expenses").GetArrayLength());
        }

        [Fact]
        public async Task ListDeleted_Row_AnswersExactlyTheTenFields()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await AddAndDeleteAsync(group, group.Owner);

            JsonElement expenses = await ReadDeletedAsync(group.Owner, group.GroupId);

            JsonElement row = Assert.Single(expenses.EnumerateArray());
            Assert.Equal(ExpenseJson.DeletedRowFieldNames, GroupRequests.PropertyNamesOf(row));
        }

        [Fact]
        public async Task ListDeleted_Row_AnswersTheExpenseValuesTheDeleteTimeAndTheRestoreDeadline()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid food = await _app.BuiltInCategoryIdAsync("food");
            ExpenseInput input = ExpenseInputs.Equal(1_550, "EUR", group.PlainRowIds[0], group.OwnerRowId, group.PlainRowIds[0]) with
            {
                Title = "Dinner",
                CategoryId = food,
                ExpenseDate = "2026-10-03",
            };
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            DateTimeOffset deletedAt = _app.Clock.GetUtcNow();
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            JsonElement expenses = await ReadDeletedAsync(group.Owner, group.GroupId);

            JsonElement row = ExpenseJson.RowWithId(expenses, expenseId);
            Assert.Equal("Dinner", row.GetProperty("title").GetString());
            Assert.Equal(1_550, row.GetProperty("amountMinor").GetInt64());
            Assert.Equal("EUR", row.GetProperty("currency").GetString());
            Assert.Equal("2026-10-03", row.GetProperty("expenseDate").GetString());
            Assert.Equal(food, row.GetProperty("categoryId").GetGuid());
            Assert.Equal("Marko", row.GetProperty("paidByName").GetString());
            Assert.Equal(deletedAt, row.GetProperty("deletedAt").GetDateTimeOffset());
            Assert.Equal(deletedAt + FiveDays, row.GetProperty("restorableUntil").GetDateTimeOffset());
            Assert.True(row.GetProperty("canEdit").GetBoolean());
        }

        [Fact]
        public async Task ListDeleted_Expenses_AreNewestDeletedFirst()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid first = await AddAndDeleteAsync(group, group.Owner);
            _app.Clock.Advance(OneMinute);
            Guid second = await AddAndDeleteAsync(group, group.Owner);
            _app.Clock.Advance(OneMinute);
            Guid third = await AddAndDeleteAsync(group, group.Owner);

            JsonElement expenses = await ReadDeletedAsync(group.Owner, group.GroupId);

            Assert.Equal([third, second, first], ExpenseJson.IdsOf(expenses));
        }

        [Fact]
        public async Task ListDeleted_ExpenseThatIsNotDeleted_IsNotInTheAnswer()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));
            Guid deleted = await AddAndDeleteAsync(group, group.Owner);

            JsonElement expenses = await ReadDeletedAsync(group.Owner, group.GroupId);

            Assert.Equal([deleted], ExpenseJson.IdsOf(expenses));
        }

        [Fact]
        public async Task ListDeleted_OneSecondBeforeFiveDays_StillHasTheExpense()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await AddAndDeleteAsync(group, group.Owner);
            _app.Clock.Advance(FiveDays - OneSecond);

            JsonElement expenses = await ReadDeletedAsync(group.Owner, group.GroupId);

            Assert.Equal([expenseId], ExpenseJson.IdsOf(expenses));
        }

        [Fact]
        public async Task ListDeleted_ExactlyFiveDaysAfterTheDelete_NoLongerHasTheExpense()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await AddAndDeleteAsync(group, group.Owner);
            _app.Clock.Advance(FiveDays);

            JsonElement expenses = await ReadDeletedAsync(group.Owner, group.GroupId);

            Assert.Equal(0, expenses.GetArrayLength());
        }

        [Fact]
        public async Task ListDeleted_ExpenseDeletedLongAgo_IsNotInTheAnswerButTheNewOneIs()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await AddAndDeleteAsync(group, group.Owner);
            _app.Clock.Advance(TimeSpan.FromDays(6));
            Guid recent = await AddAndDeleteAsync(group, group.Owner);

            JsonElement expenses = await ReadDeletedAsync(group.Owner, group.GroupId);

            Assert.Equal([recent], ExpenseJson.IdsOf(expenses));
        }

        [Fact]
        public async Task ListDeleted_EveryMemberSeesTheDeletedExpensesOfOthersWithCanEditSayingWhoMayRestore()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid byOwner = await AddAndDeleteAsync(group, group.Owner);
            Guid byMember = await AddAndDeleteAsync(group, group.Member);

            JsonElement ownersView = await ReadDeletedAsync(group.Owner, group.GroupId);
            JsonElement membersView = await ReadDeletedAsync(group.Member, group.GroupId);

            Assert.True(ExpenseJson.RowWithId(ownersView, byOwner).GetProperty("canEdit").GetBoolean());
            Assert.True(ExpenseJson.RowWithId(ownersView, byMember).GetProperty("canEdit").GetBoolean());
            Assert.False(ExpenseJson.RowWithId(membersView, byOwner).GetProperty("canEdit").GetBoolean());
            Assert.True(ExpenseJson.RowWithId(membersView, byMember).GetProperty("canEdit").GetBoolean());
        }

        [Fact]
        public async Task ListDeleted_RestoredExpense_LeavesTheList()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await AddAndDeleteAsync(group, group.Owner);
            await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId);

            JsonElement expenses = await ReadDeletedAsync(group.Owner, group.GroupId);

            Assert.Equal(0, expenses.GetArrayLength());
        }

        [Fact]
        public async Task ListDeleted_DeletedExpenseOfAnotherGroupOfTheSameCaller_IsNotInTheAnswer()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid otherGroupId = await _app.CreateGroupAsync(group.Owner);
            Guid ownerRowInOther = await GroupRows.CurrentMemberIdAsync(_app, otherGroupId, group.Owner.UserId);
            Guid inOther = await _app.AddExpenseAsync(group.Owner, otherGroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", ownerRowInOther, ownerRowInOther));
            await ExpenseRequests.DeleteAsync(group.Owner.Client, otherGroupId, inOther);

            JsonElement expenses = await ReadDeletedAsync(group.Owner, group.GroupId);

            Assert.Equal(0, expenses.GetArrayLength());
        }

        [Fact]
        public async Task ListDeleted_DeletedExpense_IsNotInTheMainList()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await AddAndDeleteAsync(group, group.Owner);

            JsonElement list = await AuthRequests.ReadJsonAsync(await ExpenseRequests.ListAsync(group.Owner.Client, group.GroupId));

            Assert.Equal(0, list.GetProperty("expenses").GetArrayLength());
        }

        private async Task<Guid> AddAndDeleteAsync(ExpenseGroup group, SignedInUser adder)
        {
            Guid expenseId = await _app.AddExpenseAsync(adder, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));
            HttpResponseMessage deleted = await ExpenseRequests.DeleteAsync(adder.Client, group.GroupId, expenseId);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

            return expenseId;
        }

        private async Task<JsonElement> ReadDeletedAsync(SignedInUser viewer, Guid groupId)
        {
            HttpResponseMessage response = await ExpenseRequests.ListDeletedAsync(viewer.Client, groupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await AuthRequests.ReadJsonAsync(response)).GetProperty("expenses");
        }
    }
}
