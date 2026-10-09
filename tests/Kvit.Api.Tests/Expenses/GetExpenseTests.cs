using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class GetExpenseTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        [Fact]
        public async Task Get_Expense_AnswersExactlyTheDetailFieldsAndTheShareAndHistoryFields()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId));

            JsonElement detail = await _app.ReadExpenseAsync(group.Member, group.GroupId, expenseId);

            Assert.Equal(ExpenseJson.DetailFieldNames, GroupRequests.PropertyNamesOf(detail));
            Assert.All(detail.GetProperty("shares").EnumerateArray(), share => Assert.Equal(ExpenseJson.ShareFieldNames, GroupRequests.PropertyNamesOf(share)));
            Assert.All(detail.GetProperty("history").EnumerateArray(), entry => Assert.Equal(ExpenseJson.HistoryFieldNames, GroupRequests.PropertyNamesOf(entry)));
        }

        [Fact]
        public async Task Get_Expense_AnswersTheSameValuesTheAddAnswered()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.PlainRowIds[0], [.. group.RowIdsInJoiningOrder]) with { Title = "Dinner", Note = "Lake" };
            HttpResponseMessage added = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input);
            JsonElement addedBody = await AuthRequests.ReadJsonAsync(added);

            JsonElement detail = await _app.ReadExpenseAsync(group.Owner, group.GroupId, addedBody.GetProperty("id").GetGuid());

            Assert.Equal(addedBody.GetRawText(), detail.GetRawText());
        }

        [Fact]
        public async Task Get_PlainNamePayer_IsNamedByThePlainName()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.PlainRowIds[0], group.OwnerRowId, group.PlainRowIds[0]));

            JsonElement detail = await _app.ReadExpenseAsync(group.Owner, group.GroupId, expenseId);

            Assert.Equal(group.PlainRowIds[0], detail.GetProperty("paidByMemberId").GetGuid());
            Assert.Equal("Marko", detail.GetProperty("paidByName").GetString());
            Assert.Equal(["Ana", "Marko"], ExpenseJson.ShareNames(detail));
        }

        [Fact]
        public async Task Get_AccountMembersAfterARename_AreNamedByTheirCurrentDisplayName()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.MemberRowId, group.OwnerRowId, group.MemberRowId));
            await UserRows.SetDisplayNameAsync(_app, group.Member.UserId, "Bojan Petrov");
            await UserRows.SetDisplayNameAsync(_app, group.Owner.UserId, "Ana Ivanova");

            JsonElement detail = await _app.ReadExpenseAsync(group.Owner, group.GroupId, expenseId);

            Assert.Equal("Bojan Petrov", detail.GetProperty("paidByName").GetString());
            Assert.Equal("Bojan Petrov", detail.GetProperty("createdByName").GetString());
            Assert.Equal(["Ana Ivanova", "Bojan Petrov"], ExpenseJson.ShareNames(detail));
        }

        [Fact]
        public async Task Get_ExpenseAddedByTheOwner_IsEditableByTheOwnerAndNotByAnotherMember()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));

            JsonElement seenByOwner = await _app.ReadExpenseAsync(group.Owner, group.GroupId, expenseId);
            JsonElement seenByMember = await _app.ReadExpenseAsync(group.Member, group.GroupId, expenseId);

            Assert.True(seenByOwner.GetProperty("canEdit").GetBoolean());
            Assert.False(seenByMember.GetProperty("canEdit").GetBoolean());
        }

        [Fact]
        public async Task Get_ExpenseAddedByAMember_IsEditableByThatMemberAndByTheOwner()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));

            JsonElement seenByOwner = await _app.ReadExpenseAsync(group.Owner, group.GroupId, expenseId);
            JsonElement seenByMember = await _app.ReadExpenseAsync(group.Member, group.GroupId, expenseId);

            Assert.True(seenByOwner.GetProperty("canEdit").GetBoolean());
            Assert.True(seenByMember.GetProperty("canEdit").GetBoolean());
        }

        [Fact]
        public async Task Get_ExpenseWithAMemberRemovedLater_StillShowsThatMemberInTheSplit()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.PlainRowIds[0]));
            await ExpenseRows.EndMemberRowAsync(_app, group.PlainRowIds[0], group.Owner.UserId);

            JsonElement detail = await _app.ReadExpenseAsync(group.Owner, group.GroupId, expenseId);

            Assert.Equal([group.OwnerRowId, group.PlainRowIds[0]], ExpenseJson.ShareMemberIds(detail));
            Assert.Equal(["Ana", "Marko"], ExpenseJson.ShareNames(detail));
        }

        [Fact]
        public async Task Get_DeletedExpense_Answers404ExpenseNotFound()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            HttpResponseMessage response = await ExpenseRequests.GetAsync(group.Owner.Client, group.GroupId, expenseId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.EXPENSE_NOT_FOUND);
        }

        [Fact]
        public async Task Get_ExpenseThatDoesNotExist_Answers404ExpenseNotFound()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();

            HttpResponseMessage response = await ExpenseRequests.GetAsync(group.Owner.Client, group.GroupId, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.EXPENSE_NOT_FOUND);
        }

        [Fact]
        public async Task Get_ExpenseOfAnotherGroupOfTheSameCaller_Answers404ExpenseNotFound()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid otherGroupId = await _app.CreateGroupAsync(group.Owner);
            Guid ownerRowInOther = await GroupRows.CurrentMemberIdAsync(_app, otherGroupId, group.Owner.UserId);
            Guid expenseInOther = await _app.AddExpenseAsync(group.Owner, otherGroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", ownerRowInOther, ownerRowInOther));

            HttpResponseMessage response = await ExpenseRequests.GetAsync(group.Owner.Client, group.GroupId, expenseInOther);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.EXPENSE_NOT_FOUND);
        }

        [Fact]
        public async Task Get_HistoryAfterAddEditDeleteAndRestore_IsNewestFirstWithTheActorsNames()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId) with { Title = "Dinner" };
            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, input);
            _app.Clock.Advance(OneMinute);
            await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { Title = "Lunch" });
            _app.Clock.Advance(OneMinute);
            await ExpenseRequests.DeleteAsync(group.Member.Client, group.GroupId, expenseId);
            _app.Clock.Advance(OneMinute);
            await ExpenseRequests.RestoreAsync(group.Owner.Client, group.GroupId, expenseId);

            JsonElement history = (await _app.ReadExpenseAsync(group.Owner, group.GroupId, expenseId)).GetProperty("history");

            Assert.Equal(["ExpenseRestored", "ExpenseDeleted", "ExpenseEdited", "ExpenseAdded"], ExpenseJson.TypesOf(history));
            Assert.Equal(["Ana", "Bojan", "Ana", "Bojan"], [.. history.EnumerateArray().Select(entry => entry.GetProperty("actorName").GetString()!)]);
        }

        [Fact]
        public async Task Get_HistoryEntries_AnswerTheTimeOfEachEvent()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            DateTimeOffset addedAt = _app.Clock.GetUtcNow();
            _app.Clock.Advance(OneMinute);
            await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { Title = "Lunch" });
            DateTimeOffset editedAt = _app.Clock.GetUtcNow();

            JsonElement history = (await _app.ReadExpenseAsync(group.Owner, group.GroupId, expenseId)).GetProperty("history");

            Assert.Equal([editedAt, addedAt], [.. history.EnumerateArray().Select(entry => entry.GetProperty("createdAt").GetDateTimeOffset())]);
        }

        [Fact]
        public async Task Get_HistoryOfAnEditedExpense_AnswersTheChangesOfTheEditAndNullChangesForTheAdd()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId) with { Title = "Dinner" };
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            _app.Clock.Advance(OneMinute);
            await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { Title = "Lunch" });

            JsonElement history = (await _app.ReadExpenseAsync(group.Owner, group.GroupId, expenseId)).GetProperty("history");

            JsonElement edited = history[0];
            JsonElement change = Assert.Single(edited.GetProperty("changes").EnumerateArray());
            Assert.Equal("title", change.GetProperty("field").GetString());
            Assert.Equal("Dinner", change.GetProperty("old").GetString());
            Assert.Equal("Lunch", change.GetProperty("new").GetString());
            Assert.Equal(JsonValueKind.Null, history[1].GetProperty("changes").ValueKind);
        }

        [Fact]
        public async Task Get_History_HoldsOnlyTheEventsOfThatExpense()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId);
            Guid first = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            Guid second = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            _app.Clock.Advance(OneMinute);
            await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, second, input with { Title = "Lunch" });

            JsonElement history = (await _app.ReadExpenseAsync(group.Owner, group.GroupId, first)).GetProperty("history");

            Assert.Equal(["ExpenseAdded"], ExpenseJson.TypesOf(history));
        }

        [Fact]
        public async Task Get_ExpenseNeverEdited_AnswersANullUpdatedAt()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));

            JsonElement detail = await _app.ReadExpenseAsync(group.Owner, group.GroupId, expenseId);

            Assert.Equal(JsonValueKind.Null, detail.GetProperty("updatedAt").ValueKind);
        }

        [Fact]
        public async Task Get_ExpenseAfterAnEdit_AnswersTheTimeOfTheEditAsUpdatedAt()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            _app.Clock.Advance(OneMinute);
            await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { Title = "Lunch" });

            JsonElement detail = await _app.ReadExpenseAsync(group.Owner, group.GroupId, expenseId);

            Assert.Equal(_app.Clock.GetUtcNow(), detail.GetProperty("updatedAt").GetDateTimeOffset());
        }

        [Fact]
        public async Task Get_ByAnotherMemberOfTheGroup_Answers200()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId));

            HttpResponseMessage response = await ExpenseRequests.GetAsync(group.Member.Client, group.GroupId, expenseId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
