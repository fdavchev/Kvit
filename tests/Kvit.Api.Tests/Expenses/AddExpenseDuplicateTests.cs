using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class AddExpenseDuplicateTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Add_SameClientRequestIdAndBodyTwice_Answers200BothTimesWithTheSameExpense()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid clientRequestId = Guid.CreateVersion7();

            HttpResponseMessage first = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input, clientRequestId);
            HttpResponseMessage second = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            JsonElement firstBody = await AuthRequests.ReadJsonAsync(first);
            JsonElement secondBody = await AuthRequests.ReadJsonAsync(second);
            Assert.Equal(firstBody.GetRawText(), secondBody.GetRawText());
        }

        [Fact]
        public async Task Add_SameClientRequestIdTwice_WritesOneExpenseOneEventAndOneUsageEvent()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid clientRequestId = Guid.CreateVersion7();
            HttpResponseMessage first = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input, clientRequestId);
            Guid expenseId = (await AuthRequests.ReadJsonAsync(first)).GetProperty("id").GetGuid();

            await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input, clientRequestId);

            Assert.Equal(1, await ExpenseRows.CountExpensesAsync(_app, group.GroupId));
            Assert.Equal(2, await ExpenseRows.CountSharesOfAsync(_app, expenseId));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ExpenseAdded"));
            Assert.Equal(1, await GroupRows.CountUsageEventsAsync(_app, group.Owner.UserId, "ExpenseAdded"));
        }

        [Fact]
        public async Task Add_SameClientRequestIdWithOtherFields_AnswersTheFirstExpenseUnchanged()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput first = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId) with { Title = "Dinner" };
            ExpenseInput other = ExpenseInputs.Equal(500_000, ExpenseInputs.Eur, group.MemberRowId, group.MemberRowId) with { Title = "Something else" };
            Guid clientRequestId = Guid.CreateVersion7();
            HttpResponseMessage firstResponse = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, first, clientRequestId);
            Guid expenseId = (await AuthRequests.ReadJsonAsync(firstResponse)).GetProperty("id").GetGuid();

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, other, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(expenseId, body.GetProperty("id").GetGuid());
            Assert.Equal("Dinner", body.GetProperty("title").GetString());
            Assert.Equal(ExpenseInputs.OneThousandMkd, body.GetProperty("amountMinor").GetInt64());
            Assert.Equal("MKD", body.GetProperty("currency").GetString());
            Assert.Equal("Dinner", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "title"));
            Assert.Equal(1, await ExpenseRows.CountExpensesAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Add_ClientRequestIdAlreadyUsedInAnotherGroup_Answers400ClientRequestIdUsedAndWritesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseGroup other = await _app.CreateExpenseGroupAsync();
            Guid clientRequestId = Guid.CreateVersion7();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId);
            await _app.AddExpenseAsync(group.Owner, group.GroupId, input, clientRequestId);
            ExpenseInput inOther = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, other.OwnerRowId, other.OwnerRowId);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(other.Owner.Client, other.GroupId, inOther, clientRequestId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_CLIENT_REQUEST_ID_USED);
            Assert.Equal(0, await ExpenseRows.CountExpensesAsync(_app, other.GroupId));
        }

        [Fact]
        public async Task Add_ClientRequestIdAlreadyUsedByAnotherPersonInTheSameGroup_Answers400ClientRequestIdUsedAndWritesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid clientRequestId = Guid.CreateVersion7();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input, clientRequestId);
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Member.Client, group.GroupId, input, clientRequestId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_CLIENT_REQUEST_ID_USED);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(group.Owner.UserId.ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "created_by_user_id"));
            Assert.Equal(0, await GroupRows.CountUsageEventsAsync(_app, group.Member.UserId, "ExpenseAdded"));
        }

        [Fact]
        public async Task Add_ClientRequestIdWhoseFirstTryWasRejected_IsFreeForTheCorrectedRequest()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid clientRequestId = Guid.CreateVersion7();
            ExpenseInput wrong = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "USD", group.OwnerRowId, group.OwnerRowId);
            ExpenseInput corrected = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId);
            HttpResponseMessage rejected = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, wrong, clientRequestId);
            await ProblemResponse.AssertAsync(rejected, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_CURRENCY_INVALID);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, corrected, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, await ExpenseRows.CountExpensesAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Add_TwoDifferentClientRequestIds_SaveTwoExpenses()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId);

            await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input, Guid.CreateVersion7());
            await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input, Guid.CreateVersion7());

            Assert.Equal(2, await ExpenseRows.CountExpensesAsync(_app, group.GroupId));
        }
    }
}
