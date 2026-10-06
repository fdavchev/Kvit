using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class OneBillDuplicateTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task OneBill_SameClientRequestIdTwice_AnswersTheFirstGroupIdAndExpenseIdBothTimes()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");
            Guid clientRequestId = Guid.CreateVersion7();

            (Guid firstGroup, Guid firstExpense) = await _app.CreateOneBillAsync(caller, input, clientRequestId);
            (Guid secondGroup, Guid secondExpense) = await _app.CreateOneBillAsync(caller, input, clientRequestId);

            Assert.Equal(firstGroup, secondGroup);
            Assert.Equal(firstExpense, secondExpense);
        }

        [Fact]
        public async Task OneBill_SameClientRequestIdTwice_WritesOneGroupOneExpenseAndOneSetOfEvents()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");
            Guid clientRequestId = Guid.CreateVersion7();
            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, input, clientRequestId);

            await _app.CreateOneBillAsync(caller, input, clientRequestId);

            Assert.Equal(1, await GroupRows.CountGroupsOwnedByAsync(_app, caller.UserId));
            Assert.Equal(1, await ExpenseRows.CountExpensesCreatedByAsync(_app, caller.UserId));
            Assert.Equal(2, await GroupRows.CountMembersAsync(_app, groupId));
            Assert.Equal(1, await GroupRows.CountUsageEventsAsync(_app, caller.UserId, "GroupCreated"));
            Assert.Equal(1, await GroupRows.CountUsageEventsAsync(_app, caller.UserId, "ExpenseAdded"));
            Assert.Equal(3, await ExpenseRows.CountEventsByActorAsync(_app, caller.UserId));
        }

        [Fact]
        public async Task OneBill_SameClientRequestIdWithOtherFields_AnswersTheFirstBillUnchanged()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput first = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko") with { Title = "Dinner" };
            OneBillInput other = OneBillInput.EqualAmongEveryone(5_000, ExpenseInputs.Eur, "Petar", "Stefan") with { Title = "Something else" };
            Guid clientRequestId = Guid.CreateVersion7();
            (Guid firstGroup, Guid firstExpense) = await _app.CreateOneBillAsync(caller, first, clientRequestId);

            (Guid groupId, Guid expenseId) = await _app.CreateOneBillAsync(caller, other, clientRequestId);

            Assert.Equal(firstGroup, groupId);
            Assert.Equal(firstExpense, expenseId);
            Assert.Equal("Dinner", await GroupRows.GroupValueAsync(_app, groupId, "name"));
            Assert.Equal("100000", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "amount_minor"));
        }

        [Fact]
        public async Task OneBill_ClientRequestIdAlreadyUsedByAnotherPerson_Answers400ClientRequestIdUsedAndWritesNothingForThem()
        {
            SignedInUser first = await _app.SignUpAsync();
            SignedInUser second = await _app.SignUpAsync("Bojan");
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");
            Guid clientRequestId = Guid.CreateVersion7();
            await _app.CreateOneBillAsync(first, input, clientRequestId);

            HttpResponseMessage response = await ExpenseRequests.OneBillAsync(second.Client, input, clientRequestId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_CLIENT_REQUEST_ID_USED);
            await _app.AssertNothingWrittenForAsync(second);
        }

        [Fact]
        public async Task OneBill_ClientRequestIdWhoseFirstTryWasRejected_IsFreeForTheCorrectedRequest()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput wrong = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, "USD", "Marko");
            OneBillInput corrected = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");
            Guid clientRequestId = Guid.CreateVersion7();
            HttpResponseMessage rejected = await ExpenseRequests.OneBillAsync(caller.Client, wrong, clientRequestId);
            await ProblemResponse.AssertAsync(rejected, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_CURRENCY_INVALID);

            HttpResponseMessage response = await ExpenseRequests.OneBillAsync(caller.Client, corrected, clientRequestId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(1, await GroupRows.CountGroupsOwnedByAsync(_app, caller.UserId));
            Assert.NotEqual(Guid.Empty, body.GetProperty("groupId").GetGuid());
        }
    }
}
