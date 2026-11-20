using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.BudgetShares;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.PossibleDuplicates
{
    public class DuplicateAnswerSameMomentTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const int Repetitions = 20;
        private const string Table = "duplicate_answers";

        [Fact]
        public async Task Put_TwoRequestsWithTheSameAnswerAtTheSameMoment_BothAnswer204AndOneRowStays()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await using InsertGate gate = await InsertGate.CloseAsync(_app, Table);

            Task<HttpResponseMessage> first = DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, expenseId, DuplicateAnswerRequests.SameAnswer);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, expenseId, DuplicateAnswerRequests.SameAnswer);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.NoContent], [.. responses.Select(response => response.StatusCode)]);
            Assert.Equal(1, await DuplicateAnswerRows.CountOfUserAsync(_app, group.Member.UserId));
            Assert.Equal(DuplicateAnswerRequests.SameAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, group.Member.UserId, entryId, expenseId));
        }

        [Fact]
        public async Task Put_SameAndNewAtTheSameMoment_BothAnswer204AndOneRowStaysWithOneOfTheTwoAnswers()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await using InsertGate gate = await InsertGate.CloseAsync(_app, Table);

            Task<HttpResponseMessage> same = DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, expenseId, DuplicateAnswerRequests.SameAnswer);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> other = DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, expenseId, DuplicateAnswerRequests.NewAnswer);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(same, other);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.NoContent], [.. responses.Select(response => response.StatusCode)]);
            Assert.Equal(1, await DuplicateAnswerRows.CountOfUserAsync(_app, group.Member.UserId));
            Assert.Contains(await DuplicateAnswerRows.AnswerOfAsync(_app, group.Member.UserId, entryId, expenseId), new[] { DuplicateAnswerRequests.SameAnswer, DuplicateAnswerRequests.NewAnswer });
        }

        [Fact]
        public async Task Put_TwoPairsAtTheSameMoment_BothAnswer204AndEachKeepsItsOwnRow()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid firstExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid secondExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, "2026-10-04");
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await using InsertGate gate = await InsertGate.CloseAsync(_app, Table);

            Task<HttpResponseMessage> first = DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, firstExpense, DuplicateAnswerRequests.SameAnswer);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, secondExpense, DuplicateAnswerRequests.NewAnswer);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.NoContent], [.. responses.Select(response => response.StatusCode)]);
            Assert.Equal(DuplicateAnswerRequests.SameAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, group.Member.UserId, entryId, firstExpense));
            Assert.Equal(DuplicateAnswerRequests.NewAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, group.Member.UserId, entryId, secondExpense));
        }

        [Fact]
        public async Task Put_SameForOneEntryWithTwoExpensesAtTheSameMoment_OneAnswers204AndTheOtherAnswers400AlreadyMerged()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid firstExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid secondExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, "2026-10-04");
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await using InsertGate gate = await InsertGate.CloseAsync(_app, Table);

            Task<HttpResponseMessage> first = DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, firstExpense, DuplicateAnswerRequests.SameAnswer);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, secondExpense, DuplicateAnswerRequests.SameAnswer);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            await AssertOneMergedAndTheOtherWasRefusedAsync(responses, group.Member.UserId);
        }

        [Fact]
        public async Task Put_SameForOneExpenseWithTwoEntriesAtTheSameMoment_OneAnswers204AndTheOtherAnswers400AlreadyMerged()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid firstEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid secondEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await using InsertGate gate = await InsertGate.CloseAsync(_app, Table);

            Task<HttpResponseMessage> first = DuplicateAnswerRequests.PutAsync(group.Member.Client, firstEntry, expenseId, DuplicateAnswerRequests.SameAnswer);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = DuplicateAnswerRequests.PutAsync(group.Member.Client, secondEntry, expenseId, DuplicateAnswerRequests.SameAnswer);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            await AssertOneMergedAndTheOtherWasRefusedAsync(responses, group.Member.UserId);
        }

        [Fact]
        public async Task Put_ManyRequestsWithTheSameAnswerAtTheSameMoment_AllAnswer204AndOneRowStays()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);

            for (int repetition = 0; repetition < Repetitions; repetition++)
            {
                HttpResponseMessage[] responses = await Task.WhenAll(
                    DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, expenseId, DuplicateAnswerRequests.SameAnswer),
                    DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, expenseId, DuplicateAnswerRequests.SameAnswer),
                    DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, expenseId, DuplicateAnswerRequests.SameAnswer));

                Assert.All(responses, response => Assert.Equal(HttpStatusCode.NoContent, response.StatusCode));
                Assert.Equal(1, await DuplicateAnswerRows.CountOfUserAsync(_app, group.Member.UserId));
                await DuplicateAnswerRows.DeleteOfUserAsync(_app, group.Member.UserId);
            }
        }

        private async Task AssertOneMergedAndTheOtherWasRefusedAsync(HttpResponseMessage[] responses, Guid userId)
        {
            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.BadRequest], [.. responses.Select(response => response.StatusCode).Order()]);
            await ProblemResponse.AssertAsync(responses.Single(response => response.StatusCode == HttpStatusCode.BadRequest), HttpStatusCode.BadRequest, DuplicateAnswerRequests.AlreadyMergedCode);
            Assert.Equal(1, await DuplicateAnswerRows.CountOfUserAsync(_app, userId));
        }
    }
}
