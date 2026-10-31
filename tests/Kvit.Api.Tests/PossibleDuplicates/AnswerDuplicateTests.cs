using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.BudgetShares;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.PossibleDuplicates
{
    public class AnswerDuplicateTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData(DuplicateAnswerRequests.SameAnswer)]
        [InlineData(DuplicateAnswerRequests.NewAnswer)]
        public async Task Put_ValidAnswer_Answers204WithNoBodyAndStoresOneRow(string answer)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, expenseId, answer);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Equal(answer, await DuplicateAnswerRows.AnswerOfAsync(_app, group.Member.UserId, entryId, expenseId));
            Assert.Equal(1, await DuplicateAnswerRows.CountOfUserAsync(_app, group.Member.UserId));
        }

        [Theory]
        [InlineData(DuplicateAnswerRequests.SameAnswer)]
        [InlineData(DuplicateAnswerRequests.NewAnswer)]
        public async Task Put_AnsweredPair_NeverComesBackInPossibleDuplicates(string answer)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Assert.Single(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));

            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, expenseId, answer);

            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Put_AnswerForOnePair_LeavesTheOtherPairOfTheSameEntryListed()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid firstExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid secondExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, "2026-10-04");
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);

            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, firstExpense, DuplicateAnswerRequests.NewAnswer);

            Assert.Equal(PossibleDuplicateSteps.Keys((entryId, secondExpense)), await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Put_SameAnswerAgain_Answers204AndChangesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, expenseId, DuplicateAnswerRequests.SameAnswer);
            string before = await DuplicateAnswerRows.SnapshotAsync(_app);
            _app.Clock.Advance(TimeSpan.FromMinutes(5));

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, expenseId, DuplicateAnswerRequests.SameAnswer);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(before, await DuplicateAnswerRows.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Put_OtherAnswerAfterSame_ChangesItToNewAndTheEntryCountsAgain()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, expenseId, DuplicateAnswerRequests.SameAnswer);
            Assert.Equal(expenseId, await PossibleDuplicateSteps.MergedWithAsync(group.Member.Client, entryId));

            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, expenseId, DuplicateAnswerRequests.NewAnswer);

            Assert.Null(await PossibleDuplicateSteps.MergedWithAsync(group.Member.Client, entryId));
            Assert.Equal(DuplicateAnswerRequests.NewAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, group.Member.UserId, entryId, expenseId));
            Assert.Equal(1, await DuplicateAnswerRows.CountOfUserAsync(_app, group.Member.UserId));
            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Put_OtherAnswerAfterNew_ChangesItToSameAndMergesTheEntry()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, expenseId, DuplicateAnswerRequests.NewAnswer);

            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, expenseId, DuplicateAnswerRequests.SameAnswer);

            Assert.Equal(expenseId, await PossibleDuplicateSteps.MergedWithAsync(group.Member.Client, entryId));
            Assert.Equal(DuplicateAnswerRequests.SameAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, group.Member.UserId, entryId, expenseId));
            Assert.Equal(1, await DuplicateAnswerRows.CountOfUserAsync(_app, group.Member.UserId));
        }

        [Fact]
        public async Task Put_PairThatDoesNotMatchTheRuleRightNow_IsStillAccepted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member, 90_000, "2026-10-01", PossibleDuplicateSteps.Transport);
            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));

            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, expenseId, DuplicateAnswerRequests.SameAnswer);

            Assert.Equal(expenseId, await PossibleDuplicateSteps.MergedWithAsync(group.Member.Client, entryId));
        }

        [Fact]
        public async Task Put_AnswerForAnEntryAndAShareInAnEarlierMonth_IsAcceptedAndMergesThere()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, "2026-09-29");
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member, entryDate: "2026-10-01");

            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, expenseId, DuplicateAnswerRequests.SameAnswer);

            Assert.Equal(expenseId, await PossibleDuplicateSteps.MergedWithAsync(group.Member.Client, entryId, "2026-10"));
        }

        [Theory]
        [InlineData("Closing")]
        [InlineData("Finished")]
        public async Task Put_InAClosingOrFinishedGroup_IsAcceptedForEveryMember(string status)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid memberEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid ownerEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Owner);
            await GroupRows.SetStatusAsync(_app, group.GroupId, status);

            await PossibleDuplicateSteps.AnswerAsync(group.Member, memberEntry, expenseId, DuplicateAnswerRequests.SameAnswer);
            await PossibleDuplicateSteps.AnswerAsync(group.Owner, ownerEntry, expenseId, DuplicateAnswerRequests.NewAnswer);

            Assert.Equal(expenseId, await PossibleDuplicateSteps.MergedWithAsync(group.Member.Client, memberEntry));
            Assert.Null(await PossibleDuplicateSteps.MergedWithAsync(group.Owner.Client, ownerEntry));
            Assert.Equal(DuplicateAnswerRequests.SameAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, group.Member.UserId, memberEntry, expenseId));
            Assert.Equal(DuplicateAnswerRequests.NewAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, group.Owner.UserId, ownerEntry, expenseId));
        }

        [Fact]
        public async Task Put_Answer_WritesNoGroupActivityAndNoUsageEvent()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            string groupBefore = await GroupRows.SnapshotAsync(_app, group.GroupId);
            int eventsBefore = await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, group.Member.UserId);

            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, expenseId, DuplicateAnswerRequests.SameAnswer);

            Assert.Equal(groupBefore, await GroupRows.SnapshotAsync(_app, group.GroupId));
            Assert.Equal(eventsBefore, await PersonalEntrySteps.CountActivityAndUsageEventsOfAsync(_app, group.Member.UserId));
        }

        [Fact]
        public async Task Put_EntryOfAnotherPerson_Answers404EntryNotFoundAndStoresNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid ownersEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Owner);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, ownersEntry, expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
            Assert.Equal(0, await DuplicateAnswerRows.CountOfUserAsync(_app, group.Member.UserId));
        }

        [Fact]
        public async Task Put_EntryThatDoesNotExist_Answers404EntryNotFound()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, Guid.CreateVersion7(), expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
        }

        [Fact]
        public async Task Put_DeletedEntry_Answers404EntryNotFoundAndStoresNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PersonalEntryRequests.DeleteAsync(group.Member.Client, entryId);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
            Assert.Equal(0, await DuplicateAnswerRows.CountOfUserAsync(_app, group.Member.UserId));
        }

        [Fact]
        public async Task Put_IncomeEntry_Answers404EntryNotFoundAndStoresNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid incomeId = await PersonalEntrySteps.AddIncomeAsync(group.Member.Client, IncomeEntryInput.Of(PossibleDuplicateSteps.ShareMinor, entryDate: ExpenseInputs.DefaultDate));

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, incomeId, expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
            Assert.Equal(0, await DuplicateAnswerRows.CountOfUserAsync(_app, group.Member.UserId));
        }

        [Fact]
        public async Task Put_ExpenseThatDoesNotExist_Answers404ExpenseNotFoundAndStoresNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.EXPENSE_NOT_FOUND);
            Assert.Equal(0, await DuplicateAnswerRows.CountOfUserAsync(_app, group.Member.UserId));
        }

        [Fact]
        public async Task Put_DeletedExpense_Answers404ExpenseNotFoundAndStoresNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.EXPENSE_NOT_FOUND);
            Assert.Equal(0, await DuplicateAnswerRows.CountOfUserAsync(_app, group.Member.UserId));
        }

        [Fact]
        public async Task Put_ExpenseOfADeletedGroup_Answers404ExpenseNotFound()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await GroupRows.SetDeletedAsync(_app, group.GroupId, group.Owner.UserId, 1);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.EXPENSE_NOT_FOUND);
        }

        [Fact]
        public async Task Put_ExpenseOfAGroupThePersonIsNotIn_Answers404ExpenseNotFoundAndStoresNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(stranger);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(stranger.Client, entryId, expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.EXPENSE_NOT_FOUND);
            Assert.Equal(0, await DuplicateAnswerRows.CountOfUserAsync(_app, stranger.UserId));
        }

        [Fact]
        public async Task Put_ExpenseWhoseShareIsNotCountedInTheBudget_Answers404ExpenseNotFound()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetShareSteps.SetAsync(group.Member, group.GroupId, false);
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Member.Client, entryId, expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.EXPENSE_NOT_FOUND);
        }

        [Fact]
        public async Task Put_ExpenseWhereThePersonsShareIsZero_Answers404ExpenseNotFound()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ShareInput[] shares = [new ShareInput(group.OwnerRowId, 0), new ShareInput(group.MemberRowId, PossibleDuplicateSteps.WholeMinor)];
            ExpenseInput input = ExpenseInputs.Of("Exact", PossibleDuplicateSteps.WholeMinor, ExpenseInputs.Mkd, group.OwnerRowId, shares) with { CategoryId = PossibleDuplicateSteps.Food };
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Owner, PossibleDuplicateSteps.WholeMinor);

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(group.Owner.Client, entryId, expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.EXPENSE_NOT_FOUND);
        }

        [Fact]
        public async Task Put_UnknownEntryAndUnknownExpense_Answers404EntryNotFoundFirst()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await DuplicateAnswerRequests.PutAsync(user.Client, Guid.CreateVersion7(), Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.PERSONAL_ENTRY_NOT_FOUND);
        }

        [Theory]
        [InlineData("\"Maybe\"")]
        [InlineData("\"\"")]
        [InlineData("\" \"")]
        [InlineData("\"Merge\"")]
        [InlineData("1")]
        [InlineData("0")]
        [InlineData("true")]
        [InlineData("null")]
        [InlineData("[\"Same\"]")]
        public async Task Put_AnswerThatIsNotSameOrNew_Answers400DuplicateAnswerInvalidAndStoresNothing(string answerJson)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            string json = $"{{\"entryId\":\"{entryId}\",\"expenseId\":\"{expenseId}\",\"answer\":{answerJson}}}";

            HttpResponseMessage response = await DuplicateAnswerRequests.PutWithJsonTextAsync(group.Member.Client, json);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, DuplicateAnswerRequests.InvalidAnswerCode);
            Assert.Equal(0, await DuplicateAnswerRows.CountOfUserAsync(_app, group.Member.UserId));
        }

        [Fact]
        public async Task Put_BodyWithoutTheAnswerProperty_Answers400DuplicateAnswerInvalid()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            string json = $"{{\"entryId\":\"{entryId}\",\"expenseId\":\"{expenseId}\"}}";

            HttpResponseMessage response = await DuplicateAnswerRequests.PutWithJsonTextAsync(group.Member.Client, json);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, DuplicateAnswerRequests.InvalidAnswerCode);
            Assert.Equal(0, await DuplicateAnswerRows.CountOfUserAsync(_app, group.Member.UserId));
        }

        [Theory]
        [InlineData("{")]
        [InlineData("")]
        [InlineData("[]")]
        [InlineData("\"Same\"")]
        [InlineData("{\"entryId\":\"not-a-guid\",\"expenseId\":\"not-a-guid\",\"answer\":\"Same\"}")]
        public async Task Put_BodyThatIsNotTheRightShape_Answers400(string json)
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await DuplicateAnswerRequests.PutWithJsonTextAsync(user.Client, json);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(0, await DuplicateAnswerRows.CountOfUserAsync(_app, user.UserId));
        }
    }
}
