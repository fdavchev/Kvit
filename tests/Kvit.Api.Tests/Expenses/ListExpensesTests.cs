using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class ListExpensesTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        [Fact]
        public async Task List_GroupWithoutExpenses_AnswersOnlyAnEmptyExpensesArray()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            HttpResponseMessage response = await ExpenseRequests.ListAsync(group.Owner.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(["expenses"], GroupRequests.PropertyNamesOf(body));
            Assert.Equal(0, body.GetProperty("expenses").GetArrayLength());
        }

        [Fact]
        public async Task List_Row_AnswersExactlyTheTenFields()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));

            JsonElement expenses = await ReadListAsync(group.Owner, group.GroupId);

            JsonElement row = Assert.Single(expenses.EnumerateArray());
            Assert.Equal(ExpenseJson.ListRowFieldNames, GroupRequests.PropertyNamesOf(row));
        }

        [Fact]
        public async Task List_Row_AnswersTheValuesOfTheExpenseAndTheCallersShare()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid food = await _app.BuiltInCategoryIdAsync("food");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.PlainRowIds[0], [.. group.RowIdsInJoiningOrder]) with
            {
                Title = "Dinner",
                CategoryId = food,
                ExpenseDate = "2026-10-03",
            };
            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, input);

            JsonElement expenses = await ReadListAsync(group.Member, group.GroupId);

            JsonElement row = ExpenseJson.RowWithId(expenses, expenseId);
            Assert.Equal("Dinner", row.GetProperty("title").GetString());
            Assert.Equal(ExpenseInputs.OneThousandMkd, row.GetProperty("amountMinor").GetInt64());
            Assert.Equal("MKD", row.GetProperty("currency").GetString());
            Assert.Equal("2026-10-03", row.GetProperty("expenseDate").GetString());
            Assert.Equal(food, row.GetProperty("categoryId").GetGuid());
            Assert.Equal(group.PlainRowIds[0], row.GetProperty("paidByMemberId").GetGuid());
            Assert.Equal("Marko", row.GetProperty("paidByName").GetString());
            Assert.Equal(33_300, row.GetProperty("yourShareMinor").GetInt64());
            Assert.True(row.GetProperty("canEdit").GetBoolean());
        }

        [Fact]
        public async Task List_ExpenseWithoutTitleAndCategory_AnswersNullsForThem()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));

            JsonElement expenses = await ReadListAsync(group.Owner, group.GroupId);

            JsonElement row = Assert.Single(expenses.EnumerateArray());
            Assert.Equal(JsonValueKind.Null, row.GetProperty("title").ValueKind);
            Assert.Equal(JsonValueKind.Null, row.GetProperty("categoryId").ValueKind);
        }

        [Fact]
        public async Task List_CallerNotInTheSplit_AnswersANullYourShare()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.MemberRowId, group.PlainRowIds[0]);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            JsonElement ownersList = await ReadListAsync(group.Owner, group.GroupId);
            JsonElement membersList = await ReadListAsync(group.Member, group.GroupId);

            Assert.Equal(JsonValueKind.Null, ExpenseJson.RowWithId(ownersList, expenseId).GetProperty("yourShareMinor").ValueKind);
            Assert.Equal(50_000, ExpenseJson.RowWithId(membersList, expenseId).GetProperty("yourShareMinor").GetInt64());
        }

        [Fact]
        public async Task List_CallerInTheSplitAtZero_AnswersAShareOfZeroNotNull()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ShareInput[] shares = [new ShareInput(group.OwnerRowId, 100_000), new ShareInput(group.MemberRowId, 0)];
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Of("Exact", ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, shares));

            JsonElement expenses = await ReadListAsync(group.Member, group.GroupId);

            Assert.Equal(0, ExpenseJson.RowWithId(expenses, expenseId).GetProperty("yourShareMinor").GetInt64());
        }

        [Fact]
        public async Task List_Expenses_AreByDateNewestFirstThenByCreationTimeNewestFirst()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId);
            Guid oldest = await _app.AddExpenseAsync(group.Owner, group.GroupId, input with { ExpenseDate = "2026-10-03" });
            _app.Clock.Advance(OneMinute);
            Guid newestDateFirstAdded = await _app.AddExpenseAsync(group.Owner, group.GroupId, input with { ExpenseDate = "2026-10-05" });
            _app.Clock.Advance(OneMinute);
            Guid newestDateLastAdded = await _app.AddExpenseAsync(group.Owner, group.GroupId, input with { ExpenseDate = "2026-10-05" });
            _app.Clock.Advance(OneMinute);
            Guid middle = await _app.AddExpenseAsync(group.Owner, group.GroupId, input with { ExpenseDate = "2026-10-04" });

            JsonElement expenses = await ReadListAsync(group.Owner, group.GroupId);

            Assert.Equal([newestDateLastAdded, newestDateFirstAdded, middle, oldest], ExpenseJson.IdsOf(expenses));
        }

        [Fact]
        public async Task List_DeletedExpense_IsNotInTheAnswer()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId);
            Guid kept = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            Guid deleted = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, deleted);

            JsonElement expenses = await ReadListAsync(group.Owner, group.GroupId);

            Assert.Equal([kept], ExpenseJson.IdsOf(expenses));
        }

        [Fact]
        public async Task List_EveryMemberSeesTheExpensesAddedByOthers()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId);
            Guid byOwner = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            Guid byMember = await _app.AddExpenseAsync(group.Member, group.GroupId, input);

            JsonElement ownersList = await ReadListAsync(group.Owner, group.GroupId);
            JsonElement membersList = await ReadListAsync(group.Member, group.GroupId);

            Assert.Equal(2, ownersList.GetArrayLength());
            Assert.Contains(byOwner, ExpenseJson.IdsOf(ownersList));
            Assert.Contains(byMember, ExpenseJson.IdsOf(ownersList));
            Assert.Equal(2, membersList.GetArrayLength());
            Assert.Contains(byOwner, ExpenseJson.IdsOf(membersList));
            Assert.Contains(byMember, ExpenseJson.IdsOf(membersList));
        }

        [Fact]
        public async Task List_CanEdit_IsTrueForWhoeverAddedItAndForTheOwnerAndFalseForOtherMembers()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId);
            Guid byOwner = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            Guid byMember = await _app.AddExpenseAsync(group.Member, group.GroupId, input);

            JsonElement ownersList = await ReadListAsync(group.Owner, group.GroupId);
            JsonElement membersList = await ReadListAsync(group.Member, group.GroupId);

            Assert.True(ExpenseJson.RowWithId(ownersList, byOwner).GetProperty("canEdit").GetBoolean());
            Assert.True(ExpenseJson.RowWithId(ownersList, byMember).GetProperty("canEdit").GetBoolean());
            Assert.False(ExpenseJson.RowWithId(membersList, byOwner).GetProperty("canEdit").GetBoolean());
            Assert.True(ExpenseJson.RowWithId(membersList, byMember).GetProperty("canEdit").GetBoolean());
        }

        [Fact]
        public async Task List_ExpensesOfAnotherGroupOfTheSameCaller_AreNotInTheAnswer()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid otherGroupId = await _app.CreateGroupAsync(group.Owner);
            Guid ownerRowInOther = await GroupRows.CurrentMemberIdAsync(_app, otherGroupId, group.Owner.UserId);
            Guid inGroup = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));
            await _app.AddExpenseAsync(group.Owner, otherGroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", ownerRowInOther, ownerRowInOther));

            JsonElement expenses = await ReadListAsync(group.Owner, group.GroupId);

            Assert.Equal([inGroup], ExpenseJson.IdsOf(expenses));
        }

        [Fact]
        public async Task List_PayerWhoWasRemovedLater_StillShowsThePayersName()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.PlainRowIds[0], group.OwnerRowId, group.PlainRowIds[0]));
            await ExpenseRows.EndMemberRowAsync(_app, group.PlainRowIds[0], group.Owner.UserId);

            JsonElement expenses = await ReadListAsync(group.Owner, group.GroupId);

            Assert.Equal("Marko", ExpenseJson.RowWithId(expenses, expenseId).GetProperty("paidByName").GetString());
        }

        private async Task<JsonElement> ReadListAsync(SignedInUser viewer, Guid groupId)
        {
            HttpResponseMessage response = await ExpenseRequests.ListAsync(viewer.Client, groupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await AuthRequests.ReadJsonAsync(response)).GetProperty("expenses");
        }
    }
}
