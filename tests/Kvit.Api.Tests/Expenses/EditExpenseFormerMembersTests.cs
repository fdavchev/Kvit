using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class EditExpenseFormerMembersTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const long TwelveHundredEurCents = 1_200;

        [Fact]
        public async Task Edit_TitleOnlyWithARemovedPersonInTheSplit_Answers204AndKeepsTheShares()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.TwelveHundredMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId, group.PlainRowIds[0]);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ExpenseRows.EndMemberRowAsync(_app, group.PlainRowIds[0], group.Owner.UserId);
            List<StoredShare> before = await ExpenseRows.SharesOfAsync(_app, expenseId);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { Title = "Lunch" });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("Lunch", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "title"));
            Assert.Equal(before, await ExpenseRows.SharesOfAsync(_app, expenseId));
            Assert.Equal(3, before.Count);
        }

        [Fact]
        public async Task Edit_TitleOnlyWithAPersonWhoLeftInTheSplit_Answers204AndKeepsTheShares()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.TwelveHundredMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, "Left");
            List<StoredShare> before = await ExpenseRows.SharesOfAsync(_app, expenseId);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { Title = "Lunch" });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("Lunch", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "title"));
            Assert.Equal(before, await ExpenseRows.SharesOfAsync(_app, expenseId));
            Assert.Contains(group.MemberRowId, before.Select(share => share.MemberId));
        }

        [Fact]
        public async Task Edit_AmountChangedOnAnEqualSplitWithARemovedPerson_MovesTheRemovedPersonsShareMoneyWithIt()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.TwelveHundredMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId, group.PlainRowIds[0]);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ExpenseRows.EndMemberRowAsync(_app, group.PlainRowIds[0], group.Owner.UserId);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { AmountMinor = 150_000 });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            List<StoredShare> stored = await ExpenseRows.SharesOfAsync(_app, expenseId);
            Assert.Equal([group.OwnerRowId, group.MemberRowId, group.PlainRowIds[0]], [.. stored.Select(share => share.MemberId)]);
            Assert.Equal([50_000L, 50_000L, 50_000L], [.. stored.Select(share => share.ShareMinor)]);
            Assert.Equal([0L, 0L, 0L], [.. stored.Select(share => share.InputValue)]);
        }

        [Fact]
        public async Task Edit_PayerIsARemovedPersonAndStaysThePayer_Answers204AndKeepsThePayer()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.TwelveHundredMkd, "MKD", group.PlainRowIds[0], group.OwnerRowId, group.PlainRowIds[0]);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ExpenseRows.EndMemberRowAsync(_app, group.PlainRowIds[0], group.Owner.UserId);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { Title = "Lunch" });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(group.PlainRowIds[0].ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "paid_by_member_id"));
            Assert.Equal("Lunch", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "title"));
        }

        [Fact]
        public async Task Edit_PayerIsARemovedPersonWhoWasNotThePayerBeforeButIsInTheSplit_Answers404MemberNotFoundAndChangesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.TwelveHundredMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.PlainRowIds[0]);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ExpenseRows.EndMemberRowAsync(_app, group.PlainRowIds[0], group.Owner.UserId);

            await AssertRejectedAsync(group, expenseId, input with { PaidByMemberId = group.PlainRowIds[0] });
        }

        [Fact]
        public async Task Edit_PayerIsAnotherRemovedPersonWhileTheOldPayerIsAlsoRemoved_Answers404MemberNotFoundAndChangesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko", "Petar");
            Guid marko = group.PlainRowIds[0];
            Guid petar = group.PlainRowIds[1];
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.TwelveHundredMkd, "MKD", marko, group.OwnerRowId, marko, petar);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ExpenseRows.EndMemberRowAsync(_app, marko, group.Owner.UserId);
            await ExpenseRows.EndMemberRowAsync(_app, petar, group.Owner.UserId);

            await AssertRejectedAsync(group, expenseId, input with { PaidByMemberId = petar });
        }

        [Fact]
        public async Task Edit_PayerIsARemovedPersonWhoIsNotInTheExpenseAtAll_Answers404MemberNotFoundAndChangesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.TwelveHundredMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ExpenseRows.EndMemberRowAsync(_app, group.PlainRowIds[0], group.Owner.UserId);

            await AssertRejectedAsync(group, expenseId, input with { PaidByMemberId = group.PlainRowIds[0] });
        }

        [Fact]
        public async Task Edit_ExactAmountOfARemovedPersonChanged_Answers404MemberNotFoundAndChangesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid marko = group.PlainRowIds[0];
            ExpenseInput input = ExpenseInputs.Of("Exact", ExpenseInputs.TwelveHundredMkd, "MKD", group.OwnerRowId, new ShareInput(group.OwnerRowId, 50_000), new ShareInput(group.MemberRowId, 20_000), new ShareInput(marko, 50_000));
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ExpenseRows.EndMemberRowAsync(_app, marko, group.Owner.UserId);
            ExpenseInput changed = ExpenseInputs.Of("Exact", ExpenseInputs.TwelveHundredMkd, "MKD", group.OwnerRowId, new ShareInput(group.OwnerRowId, 40_000), new ShareInput(group.MemberRowId, 20_000), new ShareInput(marko, 60_000));

            await AssertRejectedAsync(group, expenseId, changed);
        }

        [Fact]
        public async Task Edit_ExactAmountOfOtherPeopleChangedWhileTheRemovedPersonsStays_Answers204AndKeepsTheRemovedPersonsAmount()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid marko = group.PlainRowIds[0];
            ExpenseInput input = ExpenseInputs.Of("Exact", ExpenseInputs.TwelveHundredMkd, "MKD", group.OwnerRowId, new ShareInput(group.OwnerRowId, 50_000), new ShareInput(group.MemberRowId, 20_000), new ShareInput(marko, 50_000));
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ExpenseRows.EndMemberRowAsync(_app, marko, group.Owner.UserId);
            ExpenseInput changed = ExpenseInputs.Of("Exact", ExpenseInputs.TwelveHundredMkd, "MKD", group.OwnerRowId, new ShareInput(group.OwnerRowId, 40_000), new ShareInput(group.MemberRowId, 30_000), new ShareInput(marko, 50_000));

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, changed);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            List<StoredShare> stored = await ExpenseRows.SharesOfAsync(_app, expenseId);
            Assert.Equal([40_000L, 30_000L, 50_000L], [.. stored.Select(share => share.ShareMinor)]);
        }

        [Fact]
        public async Task Edit_SharesCountOfARemovedPersonChanged_Answers404MemberNotFoundAndChangesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid marko = group.PlainRowIds[0];
            ExpenseInput input = ExpenseInputs.Of("Shares", ExpenseInputs.TwelveHundredMkd, "MKD", group.OwnerRowId, new ShareInput(group.OwnerRowId, 1), new ShareInput(group.MemberRowId, 1), new ShareInput(marko, 1));
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ExpenseRows.EndMemberRowAsync(_app, marko, group.Owner.UserId);
            ExpenseInput changed = ExpenseInputs.Of("Shares", ExpenseInputs.TwelveHundredMkd, "MKD", group.OwnerRowId, new ShareInput(group.OwnerRowId, 1), new ShareInput(group.MemberRowId, 1), new ShareInput(marko, 2));

            await AssertRejectedAsync(group, expenseId, changed);
        }

        [Fact]
        public async Task Edit_EqualExtraOfARemovedPersonChanged_Answers404MemberNotFoundAndChangesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid marko = group.PlainRowIds[0];
            ExpenseInput input = ExpenseInputs.Of("Equal", ExpenseInputs.TwelveHundredMkd, "MKD", group.OwnerRowId, new ShareInput(group.OwnerRowId, 0), new ShareInput(group.MemberRowId, 0), new ShareInput(marko, 30_000));
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ExpenseRows.EndMemberRowAsync(_app, marko, group.Owner.UserId);
            ExpenseInput changed = ExpenseInputs.Of("Equal", ExpenseInputs.TwelveHundredMkd, "MKD", group.OwnerRowId, new ShareInput(group.OwnerRowId, 0), new ShareInput(group.MemberRowId, 0), new ShareInput(marko, 0));

            await AssertRejectedAsync(group, expenseId, changed);
        }

        [Fact]
        public async Task Edit_EqualExtraOfARemovedPersonUnchanged_Answers204AndKeepsTheExtra()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid marko = group.PlainRowIds[0];
            ExpenseInput input = ExpenseInputs.Of("Equal", ExpenseInputs.TwelveHundredMkd, "MKD", group.OwnerRowId, new ShareInput(group.OwnerRowId, 0), new ShareInput(group.MemberRowId, 0), new ShareInput(marko, 30_000));
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ExpenseRows.EndMemberRowAsync(_app, marko, group.Owner.UserId);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { Title = "Lunch" });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            List<StoredShare> stored = await ExpenseRows.SharesOfAsync(_app, expenseId);
            Assert.Equal([30_000L, 30_000L, 60_000L], [.. stored.Select(share => share.ShareMinor)]);
            Assert.Equal([0L, 0L, 30_000L], [.. stored.Select(share => share.InputValue)]);
        }

        [Fact]
        public async Task Edit_SplitTypeChangedWhileARemovedPersonIsInTheSplit_Answers404MemberNotFoundAndChangesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid marko = group.PlainRowIds[0];
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.TwelveHundredMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId, marko);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ExpenseRows.EndMemberRowAsync(_app, marko, group.Owner.UserId);
            ExpenseInput changed = ExpenseInputs.Of("Shares", ExpenseInputs.TwelveHundredMkd, "MKD", group.OwnerRowId, new ShareInput(group.OwnerRowId, 1), new ShareInput(group.MemberRowId, 1), new ShareInput(marko, 1));

            await AssertRejectedAsync(group, expenseId, changed);
        }

        [Fact]
        public async Task Edit_ARemovedPersonWhoWasNotInTheExpenseAddedToTheSplit_Answers404MemberNotFoundAndChangesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.TwelveHundredMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ExpenseRows.EndMemberRowAsync(_app, group.PlainRowIds[0], group.Owner.UserId);

            await AssertRejectedAsync(group, expenseId, ExpenseInputs.Equal(ExpenseInputs.TwelveHundredMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId, group.PlainRowIds[0]));
        }

        [Fact]
        public async Task Edit_ARemovedPersonTakenOutOfTheSplit_Answers204AndDropsTheirShare()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.TwelveHundredMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId, group.PlainRowIds[0]);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ExpenseRows.EndMemberRowAsync(_app, group.PlainRowIds[0], group.Owner.UserId);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, ExpenseInputs.Equal(ExpenseInputs.TwelveHundredMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            List<StoredShare> stored = await ExpenseRows.SharesOfAsync(_app, expenseId);
            Assert.Equal([group.OwnerRowId, group.MemberRowId], [.. stored.Select(share => share.MemberId)]);
            Assert.Equal([60_000L, 60_000L], [.. stored.Select(share => share.ShareMinor)]);
        }

        [Fact]
        public async Task Edit_MemberWhoWasNeverInTheGroupAddedToTheSplit_StillAnswers404MemberNotFound()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.TwelveHundredMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            await AssertRejectedAsync(group, expenseId, ExpenseInputs.Equal(ExpenseInputs.TwelveHundredMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId, Guid.CreateVersion7()));
        }

        [Fact]
        public async Task Add_SplitWithARemovedPersonWhoIsInAnotherExpenseWithTheSameValue_StillAnswers404MemberNotFoundAndWritesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.TwelveHundredMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId, group.PlainRowIds[0]);
            await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ExpenseRows.EndMemberRowAsync(_app, group.PlainRowIds[0], group.Owner.UserId);
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Add_PayerWhoIsARemovedPersonPaidAnotherExpense_StillAnswers404MemberNotFoundAndWritesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.TwelveHundredMkd, "MKD", group.PlainRowIds[0], group.OwnerRowId, group.PlainRowIds[0]);
            await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ExpenseRows.EndMemberRowAsync(_app, group.PlainRowIds[0], group.Owner.UserId);
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Edit_TypedSplitChangedWithARemovedPersonKept_WritesOneSplitEntryWithTheRemovedPersonsName()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid marko = group.PlainRowIds[0];
            ExpenseInput input = ExpenseInputs.Of("Exact", TwelveHundredEurCents, "EUR", group.OwnerRowId, new ShareInput(group.OwnerRowId, 500), new ShareInput(group.MemberRowId, 200), new ShareInput(marko, 500));
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ExpenseRows.EndMemberRowAsync(_app, marko, group.Owner.UserId);
            ExpenseInput changed = ExpenseInputs.Of("Exact", TwelveHundredEurCents, "EUR", group.OwnerRowId, new ShareInput(group.OwnerRowId, 600), new ShareInput(group.MemberRowId, 100), new ShareInput(marko, 500));

            List<(string Field, string Old, string New)> changes = await EditAndReadChangesAsync(group, expenseId, changed);

            (string Field, string Old, string New) change = Assert.Single(changes);
            Assert.Equal("split", change.Field);
            Assert.Equal("Exact: Ana 500, Bojan 200, Marko 500", change.Old);
            Assert.Equal("Exact: Ana 600, Bojan 100, Marko 500", change.New);
        }

        [Fact]
        public async Task Edit_PayerMovedFromARemovedPersonToACurrentOne_WritesThePayersNamesAndTheEditorAsActor()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(TwelveHundredEurCents, "EUR", group.PlainRowIds[0], group.OwnerRowId, group.PlainRowIds[0]);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ExpenseRows.EndMemberRowAsync(_app, group.PlainRowIds[0], group.Owner.UserId);

            List<(string Field, string Old, string New)> changes = await EditAndReadChangesAsync(group, expenseId, input with { PaidByMemberId = group.OwnerRowId });

            (string Field, string Old, string New) change = Assert.Single(changes);
            Assert.Equal("paidBy", change.Field);
            Assert.Equal("Marko", change.Old);
            Assert.Equal(GroupsApp.OwnerName, change.New);
            Assert.Equal(group.Owner.UserId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "ExpenseEdited", "actor_user_id"));
        }

        [Fact]
        public async Task Activity_EditWithARemovedPersonInTheSplit_AnswersTheEditorsNameAndTheChanges()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(TwelveHundredEurCents, "EUR", group.OwnerRowId, group.OwnerRowId, group.PlainRowIds[0]);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ExpenseRows.EndMemberRowAsync(_app, group.PlainRowIds[0], group.Owner.UserId);
            await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { Title = "Lunch" });

            HttpResponseMessage response = await ExpenseRequests.ActivityAsync(group.Member.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement entry = (await AuthRequests.ReadJsonAsync(response)).GetProperty("events")[0];
            Assert.Equal("ExpenseEdited", entry.GetProperty("type").GetString());
            Assert.Equal(GroupsApp.OwnerName, entry.GetProperty("actorName").GetString());
            JsonElement title = Assert.Single(entry.GetProperty("changes").EnumerateArray());
            Assert.Equal("title", title.GetProperty("field").GetString());
            Assert.Equal("Lunch", title.GetProperty("new").GetString());
        }

        private async Task AssertRejectedAsync(ExpenseGroup group, Guid expenseId, ExpenseInput input)
        {
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }

        private async Task<List<(string Field, string Old, string New)>> EditAndReadChangesAsync(ExpenseGroup group, Guid expenseId, ExpenseInput changed)
        {
            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, changed);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            string? changesJson = await GroupRows.EventValueAsync(_app, group.GroupId, "ExpenseEdited", "changes");
            return ActivityChanges.Parse(changesJson);
        }
    }
}
