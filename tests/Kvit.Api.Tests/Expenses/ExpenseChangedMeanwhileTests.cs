using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Balances;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class ExpenseChangedMeanwhileTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string EditedEvent = "ExpenseEdited";
        private const string DeletedEvent = "ExpenseDeleted";
        private const string RestoredEvent = "ExpenseRestored";
        private const string NewTitle = "Dinner";
        private const string TitleAndAmount = "coalesce(title, '') || '|' || amount_minor";
        private const string TitleAndIsDeleted = "coalesce(title, '') || '|' || (deleted_at IS NOT NULL)";

        [Fact]
        public async Task Edit_TwoEditsAtTheSameMoment_OneAnswers204AndTheOtherAnswers409ChangedMeanwhileAndOnlyTheFirstIsSaved()
        {
            (ExpenseGroup group, ExpenseInput original, Guid expenseId) = await AddExpenseAsync();
            await using InsertGate gate = await HoldAtAsync(EditedEvent);

            Task<HttpResponseMessage> newTitle = ExpenseRequests.EditAsync(group.Member.Client, group.GroupId, expenseId, original with { Title = NewTitle });
            Task<HttpResponseMessage> newAmount = ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, original with { AmountMinor = ExpenseInputs.TwelveHundredMkd });
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(newTitle, newAmount);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.Conflict], [.. responses.Select(response => response.StatusCode).Order()]);
            await ProblemResponse.AssertAsync(responses.Single(response => response.StatusCode == HttpStatusCode.Conflict), HttpStatusCode.Conflict, ResultCodes.EXPENSE_CHANGED_MEANWHILE);
            string expected = responses[0].StatusCode == HttpStatusCode.NoContent ? $"{NewTitle}|{ExpenseInputs.OneThousandMkd}" : $"|{ExpenseInputs.TwelveHundredMkd}";
            Assert.Equal(expected, await ExpenseRows.ExpenseValueAsync(_app, expenseId, TitleAndAmount));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, EditedEvent));
        }

        [Fact]
        public async Task Edit_TwoSplitOnlyEditsByTheSamePersonAtTheSameMomentAfterAnEarlierEdit_OneAnswers204AndTheOtherAnswers409ChangedMeanwhile()
        {
            (ExpenseGroup group, ExpenseInput original, Guid expenseId) = await AddExpenseAsync();
            ExpenseInput named = original with { Title = NewTitle };
            Assert.Equal(HttpStatusCode.NoContent, (await ExpenseRequests.EditAsync(group.Member.Client, group.GroupId, expenseId, named)).StatusCode);
            await using InsertGate gate = await HoldAtAsync(EditedEvent);

            Task<HttpResponseMessage> first = ExpenseRequests.EditAsync(group.Member.Client, group.GroupId, expenseId, ExactSplit(named, group, 70_000, 30_000));
            Task<HttpResponseMessage> second = ExpenseRequests.EditAsync(group.Member.Client, group.GroupId, expenseId, ExactSplit(named, group, 20_000, 80_000));
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.Conflict], [.. responses.Select(response => response.StatusCode).Order()]);
            long expectedOwnerShare = responses[0].StatusCode == HttpStatusCode.NoContent ? 70_000 : 20_000;
            List<StoredShare> shares = await ExpenseRows.SharesOfAsync(_app, expenseId);
            Assert.Equal(expectedOwnerShare, shares.Single(share => share.MemberId == group.OwnerRowId).ShareMinor);
            Assert.Equal(ExpenseInputs.OneThousandMkd, shares.Sum(share => share.ShareMinor));
            Assert.Equal(2, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, EditedEvent));
        }

        [Fact]
        public async Task Edit_WhileADeleteOfTheSameExpenseIsSavedFirst_Answers409ChangedMeanwhileAndWritesNoHistoryLine()
        {
            (ExpenseGroup group, ExpenseInput original, Guid expenseId) = await AddExpenseAsync();
            await using InsertGate gate = await HoldAtAsync(EditedEvent);

            Task<HttpResponseMessage> edit = ExpenseRequests.EditAsync(group.Member.Client, group.GroupId, expenseId, original with { Title = NewTitle });
            await gate.WaitUntilRequestsAreHeldAsync(1);
            HttpResponseMessage deleted = await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);
            await gate.OpenAsync();
            HttpResponseMessage edited = await edit;

            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
            await ProblemResponse.AssertAsync(edited, HttpStatusCode.Conflict, ResultCodes.EXPENSE_CHANGED_MEANWHILE);
            Assert.Equal("|true", await ExpenseRows.ExpenseValueAsync(_app, expenseId, TitleAndIsDeleted));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, EditedEvent));
        }

        [Fact]
        public async Task Delete_WhileAnEditOfTheSameExpenseIsSavedFirst_Answers409ChangedMeanwhileAndKeepsTheEditedExpense()
        {
            (ExpenseGroup group, ExpenseInput original, Guid expenseId) = await AddExpenseAsync();
            await using InsertGate gate = await HoldAtAsync(DeletedEvent);

            Task<HttpResponseMessage> delete = ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            HttpResponseMessage edited = await ExpenseRequests.EditAsync(group.Member.Client, group.GroupId, expenseId, original with { Title = NewTitle });
            await gate.OpenAsync();
            HttpResponseMessage deleted = await delete;

            Assert.Equal(HttpStatusCode.NoContent, edited.StatusCode);
            await ProblemResponse.AssertAsync(deleted, HttpStatusCode.Conflict, ResultCodes.EXPENSE_CHANGED_MEANWHILE);
            Assert.Equal($"{NewTitle}|false", await ExpenseRows.ExpenseValueAsync(_app, expenseId, TitleAndIsDeleted));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, DeletedEvent));
        }

        [Fact]
        public async Task Delete_TwoDeletesAtTheSameMoment_OneAnswers204AndTheOtherAnswers404NotFoundWithOneHistoryLine()
        {
            (ExpenseGroup group, _, Guid expenseId) = await AddExpenseAsync();
            await using InsertGate gate = await HoldAtAsync(DeletedEvent);

            Task<HttpResponseMessage> first = ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);
            Task<HttpResponseMessage> second = ExpenseRequests.DeleteAsync(group.Member.Client, group.GroupId, expenseId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.NotFound], [.. responses.Select(response => response.StatusCode).Order()]);
            await ProblemResponse.AssertAsync(responses.Single(response => response.StatusCode == HttpStatusCode.NotFound), HttpStatusCode.NotFound, ResultCodes.EXPENSE_NOT_FOUND);
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, DeletedEvent));
        }

        [Fact]
        public async Task Restore_TwoRestoresAtTheSameMoment_OneAnswers204AndTheOtherAnswers400NotDeletedWithOneHistoryLine()
        {
            (ExpenseGroup group, _, Guid expenseId) = await AddExpenseAsync();
            Assert.Equal(HttpStatusCode.NoContent, (await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId)).StatusCode);
            await using InsertGate gate = await HoldAtAsync(RestoredEvent);

            Task<HttpResponseMessage> first = ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId);
            Task<HttpResponseMessage> second = ExpenseRequests.RestoreAsync(group.Member.Client, group.GroupId, expenseId);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.BadRequest], [.. responses.Select(response => response.StatusCode).Order()]);
            await ProblemResponse.AssertAsync(responses.Single(response => response.StatusCode == HttpStatusCode.BadRequest), HttpStatusCode.BadRequest, ResultCodes.EXPENSE_NOT_DELETED);
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, RestoredEvent));
        }

        [Fact]
        public async Task Edit_ThreeEditsAtTheSameMoment_OneAnswers204AndTheOtherTwoAnswer409WithOneHistoryLine()
        {
            (ExpenseGroup group, ExpenseInput original, Guid expenseId) = await AddExpenseAsync();
            await using InsertGate gate = await HoldAtAsync(EditedEvent);

            Task<HttpResponseMessage> first = ExpenseRequests.EditAsync(group.Member.Client, group.GroupId, expenseId, original with { Title = "One" });
            Task<HttpResponseMessage> second = ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, original with { Title = "Two" });
            Task<HttpResponseMessage> third = ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, original with { Title = "Three" });
            await gate.WaitUntilRequestsAreHeldAsync(3);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second, third);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.Conflict, HttpStatusCode.Conflict], [.. responses.Select(response => response.StatusCode).Order()]);
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, EditedEvent));
        }

        [Fact]
        public async Task Restore_ThreeRestoresAtTheSameMoment_OneAnswers204AndTheOtherTwoAnswer400NotDeleted()
        {
            (ExpenseGroup group, _, Guid expenseId) = await AddExpenseAsync();
            Assert.Equal(HttpStatusCode.NoContent, (await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId)).StatusCode);
            await using InsertGate gate = await HoldAtAsync(RestoredEvent);

            Task<HttpResponseMessage> first = ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId);
            Task<HttpResponseMessage> second = ExpenseRequests.RestoreAsync(group.Member.Client, group.GroupId, expenseId);
            Task<HttpResponseMessage> third = ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId);
            await gate.WaitUntilRequestsAreHeldAsync(3);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second, third);

            Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.BadRequest, HttpStatusCode.BadRequest], [.. responses.Select(response => response.StatusCode).Order()]);
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, RestoredEvent));
        }

        [Fact]
        public async Task Edit_TwoEditsOfDifferentFieldsAtTheSameMoment_TheHistoryLineHoldsOnlyTheWinnersChangeWithItsOldValue()
        {
            (ExpenseGroup group, ExpenseInput original, Guid expenseId) = await AddExpenseAsync();
            await using InsertGate gate = await HoldAtAsync(EditedEvent);

            Task<HttpResponseMessage> newTitle = ExpenseRequests.EditAsync(group.Member.Client, group.GroupId, expenseId, original with { Title = NewTitle });
            Task<HttpResponseMessage> newAmount = ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, original with { AmountMinor = ExpenseInputs.TwelveHundredMkd });
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(newTitle, newAmount);

            bool titleWon = responses[0].StatusCode == HttpStatusCode.NoContent;
            List<(string Field, string Old, string New)> changes = ActivityChanges.Parse(await GroupRows.EventValueAsync(_app, group.GroupId, EditedEvent, "changes"));
            (string Field, string Old, string New) change = Assert.Single(changes);
            Assert.Equal(titleWon ? "title" : "amount", change.Field);
            Assert.Equal(titleWon ? string.Empty : ExpenseInputs.OneThousandMkd.ToString(), change.Old);
            Assert.Equal(titleWon ? NewTitle : ExpenseInputs.TwelveHundredMkd.ToString(), change.New);
        }

        [Fact]
        public async Task Edit_TwoEditsOfDifferentFieldsAtTheSameMoment_TheBalancesMatchOnlyTheWinner()
        {
            (ExpenseGroup group, ExpenseInput original, Guid expenseId) = await AddExpenseAsync();
            await using InsertGate gate = await HoldAtAsync(EditedEvent);

            Task<HttpResponseMessage> newTitle = ExpenseRequests.EditAsync(group.Member.Client, group.GroupId, expenseId, original with { Title = NewTitle });
            Task<HttpResponseMessage> newAmount = ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, original with { AmountMinor = ExpenseInputs.TwelveHundredMkd });
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(newTitle, newAmount);

            long ownerBalance = responses[0].StatusCode == HttpStatusCode.NoContent ? 50_000 : 60_000;
            JsonElement mkd = BalanceJson.CurrencyOf(await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId), ExpenseInputs.Mkd);
            Assert.Equal([-ownerBalance, ownerBalance], [.. BalanceJson.BalanceMinors(mkd).Order()]);
        }

        [Fact]
        public async Task Edit_WhileTheExpenseIsDeletedAndRestoredMeanwhile_Answers409ChangedMeanwhileAndSavesNothing()
        {
            (ExpenseGroup group, ExpenseInput original, Guid expenseId) = await AddExpenseAsync();
            await using InsertGate gate = await HoldAtAsync(EditedEvent);

            Task<HttpResponseMessage> edit = ExpenseRequests.EditAsync(group.Member.Client, group.GroupId, expenseId, original with { Title = NewTitle });
            await gate.WaitUntilRequestsAreHeldAsync(1);
            HttpResponseMessage deleted = await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);
            HttpResponseMessage restored = await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId);
            await gate.OpenAsync();
            HttpResponseMessage edited = await edit;

            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, restored.StatusCode);
            await ProblemResponse.AssertAsync(edited, HttpStatusCode.Conflict, ResultCodes.EXPENSE_CHANGED_MEANWHILE);
            Assert.Equal("|false", await ExpenseRows.ExpenseValueAsync(_app, expenseId, TitleAndIsDeleted));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, EditedEvent));
        }

        [Fact]
        public async Task Restore_WhileTheSameExpenseIsRestoredAndDeletedAgainMeanwhile_RunsAgainAndRestoresIt()
        {
            (ExpenseGroup group, _, Guid expenseId) = await AddExpenseAsync();
            Assert.Equal(HttpStatusCode.NoContent, (await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId)).StatusCode);
            await using InsertGate gate = await HoldAtByAsync(RestoredEvent, group.Owner.UserId);

            Task<HttpResponseMessage> slowRestore = ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            HttpResponseMessage restored = await ExpenseRequests.RestoreAsync(group.Member.Client, group.GroupId, expenseId);
            HttpResponseMessage deletedAgain = await ExpenseRequests.DeleteAsync(group.Member.Client, group.GroupId, expenseId);
            await gate.OpenAsync();
            HttpResponseMessage slowResult = await slowRestore;

            Assert.Equal(HttpStatusCode.NoContent, restored.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, deletedAgain.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, slowResult.StatusCode);
            Assert.Equal("|false", await ExpenseRows.ExpenseValueAsync(_app, expenseId, TitleAndIsDeleted));
            Assert.Equal(2, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, RestoredEvent));
            Assert.Equal(2, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, DeletedEvent));
        }

        [Fact]
        public async Task Delete_WhileTheSameExpenseIsDeletedAndRestoredMeanwhile_Answers409ChangedMeanwhileAndKeepsItLive()
        {
            (ExpenseGroup group, _, Guid expenseId) = await AddExpenseAsync();
            await using InsertGate gate = await HoldAtByAsync(DeletedEvent, group.Owner.UserId);

            Task<HttpResponseMessage> slowDelete = ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            HttpResponseMessage deleted = await ExpenseRequests.DeleteAsync(group.Member.Client, group.GroupId, expenseId);
            HttpResponseMessage restored = await ExpenseRequests.RestoreAsync(group.Member.Client, group.GroupId, expenseId);
            await gate.OpenAsync();
            HttpResponseMessage slowResult = await slowDelete;

            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, restored.StatusCode);
            await ProblemResponse.AssertAsync(slowResult, HttpStatusCode.Conflict, ResultCodes.EXPENSE_CHANGED_MEANWHILE);
            Assert.Equal("|false", await ExpenseRows.ExpenseValueAsync(_app, expenseId, TitleAndIsDeleted));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, DeletedEvent));
        }

        private static ExpenseInput ExactSplit(ExpenseInput input, ExpenseGroup group, long ownerShareMinor, long memberShareMinor)
        {
            return input with { SplitType = "Exact", Shares = [new ShareInput(group.OwnerRowId, ownerShareMinor), new ShareInput(group.MemberRowId, memberShareMinor)] };
        }

        private async Task<(ExpenseGroup Group, ExpenseInput Original, Guid ExpenseId)> AddExpenseAsync()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput original = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, original);

            return (group, original, expenseId);
        }

        private Task<InsertGate> HoldAtAsync(string eventType)
        {
            return InsertGate.CloseAsync(_app, "activity_events", $"NEW.type = '{eventType}'");
        }

        private Task<InsertGate> HoldAtByAsync(string eventType, Guid actorUserId)
        {
            return InsertGate.CloseAsync(_app, "activity_events", $"NEW.type = '{eventType}' AND NEW.actor_user_id = '{actorUserId}'");
        }
    }
}
