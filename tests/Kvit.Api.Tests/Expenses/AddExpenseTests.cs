using System.Globalization;
using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class AddExpenseTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Add_ValidRequest_Answers200WithExactlyTheDetailFields()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(ExpenseJson.DetailFieldNames, GroupRequests.PropertyNamesOf(body));
        }

        [Fact]
        public async Task Add_ValidRequest_AnswersTheSavedValues()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid food = await _app.BuiltInCategoryIdAsync("food");
            ExpenseInput input = ExpenseInputs.Equal(120_000, ExpenseInputs.Mkd, group.MemberRowId, group.OwnerRowId, group.MemberRowId) with
            {
                Title = "Dinner",
                Note = "Taverna by the lake",
                CategoryId = food,
            };

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input);

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.NotEqual(Guid.Empty, body.GetProperty("id").GetGuid());
            Assert.Equal(group.GroupId, body.GetProperty("groupId").GetGuid());
            Assert.Equal("Dinner", body.GetProperty("title").GetString());
            Assert.Equal("Taverna by the lake", body.GetProperty("note").GetString());
            Assert.Equal(120_000, body.GetProperty("amountMinor").GetInt64());
            Assert.Equal("MKD", body.GetProperty("currency").GetString());
            Assert.Equal("2026-10-05", body.GetProperty("expenseDate").GetString());
            Assert.Equal(food, body.GetProperty("categoryId").GetGuid());
            Assert.Equal(group.MemberRowId, body.GetProperty("paidByMemberId").GetGuid());
            Assert.Equal(GroupsApp.MemberName, body.GetProperty("paidByName").GetString());
            Assert.Equal("Equal", body.GetProperty("splitType").GetString());
            Assert.Equal(group.Owner.UserId, body.GetProperty("createdByUserId").GetGuid());
            Assert.Equal(GroupsApp.OwnerName, body.GetProperty("createdByName").GetString());
            Assert.Equal(_app.Clock.GetUtcNow(), body.GetProperty("createdAt").GetDateTimeOffset());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("updatedAt").ValueKind);
            Assert.True(body.GetProperty("canEdit").GetBoolean());
        }

        [Fact]
        public async Task Add_ValidRequest_AnswersTheSharesInJoiningOrderWithNamesAndAmounts()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.PlainRowIds[0], group.OwnerRowId, group.MemberRowId);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input);

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            JsonElement shares = body.GetProperty("shares");
            Assert.All(shares.EnumerateArray(), share => Assert.Equal(ExpenseJson.ShareFieldNames, GroupRequests.PropertyNamesOf(share)));
            Assert.Equal([group.OwnerRowId, group.MemberRowId, group.PlainRowIds[0]], ExpenseJson.ShareMemberIds(body));
            Assert.Equal([GroupsApp.OwnerName, GroupsApp.MemberName, "Marko"], ExpenseJson.ShareNames(body));
            Assert.Equal([33_400L, 33_300L, 33_300L], ExpenseJson.ShareMinors(body));
        }

        [Fact]
        public async Task Add_ValidRequest_AnswersAHistoryWithOneExpenseAddedEntryByTheAdder()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Member.Client, group.GroupId, input);

            JsonElement history = (await AuthRequests.ReadJsonAsync(response)).GetProperty("history");
            JsonElement entry = Assert.Single(history.EnumerateArray());
            Assert.Equal(ExpenseJson.HistoryFieldNames, GroupRequests.PropertyNamesOf(entry));
            Assert.Equal("ExpenseAdded", entry.GetProperty("type").GetString());
            Assert.Equal(GroupsApp.MemberName, entry.GetProperty("actorName").GetString());
            Assert.Equal(_app.Clock.GetUtcNow(), entry.GetProperty("createdAt").GetDateTimeOffset());
        }

        [Fact]
        public async Task Add_ValidRequest_StoresTheExpenseRow()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(120_000, ExpenseInputs.Mkd, group.MemberRowId, group.OwnerRowId, group.MemberRowId) with { Title = "Dinner", Note = "Lake" };
            Guid clientRequestId = Guid.CreateVersion7();

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input, clientRequestId);

            Guid expenseId = (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid();
            Assert.Equal(group.GroupId.ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "group_id"));
            Assert.Equal("Dinner", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "title"));
            Assert.Equal("Lake", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "note"));
            Assert.Equal("120000", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "amount_minor"));
            Assert.Equal("MKD", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "currency"));
            Assert.Equal("2026-10-05", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "expense_date"));
            Assert.Null(await ExpenseRows.ExpenseValueAsync(_app, expenseId, "category_id"));
            Assert.Equal(group.MemberRowId.ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "paid_by_member_id"));
            Assert.Equal("Equal", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "split_type"));
            Assert.Equal(group.Owner.UserId.ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "created_by_user_id"));
            Assert.Equal(clientRequestId.ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "client_request_id"));
            Assert.Null(await ExpenseRows.ExpenseValueAsync(_app, expenseId, "updated_at"));
            Assert.Null(await ExpenseRows.ExpenseValueAsync(_app, expenseId, "updated_by_user_id"));
            Assert.Null(await ExpenseRows.ExpenseValueAsync(_app, expenseId, "deleted_at"));
            Assert.Null(await ExpenseRows.ExpenseValueAsync(_app, expenseId, "deleted_by_user_id"));
        }

        [Fact]
        public async Task Add_ValidRequest_StoresTheCreatedAtFromTheClock()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId);

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            string expected = _app.Clock.GetUtcNow().UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            Assert.Equal(expected, await ExpenseRows.ExpenseValueAsync(_app, expenseId, "created_at AT TIME ZONE 'UTC'"));
        }

        [Fact]
        public async Task Add_TitleWithSurroundingSpaces_StoresTheTrimmedTitle()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId) with { Title = "  Dinner  " };

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            Assert.Equal("Dinner", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "title"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Add_EmptyOrBlankTitle_StoresNull(string title)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId) with { Title = title };

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            Assert.Null(await ExpenseRows.ExpenseValueAsync(_app, expenseId, "title"));
            JsonElement detail = await _app.ReadExpenseAsync(group.Owner, group.GroupId, expenseId);
            Assert.Equal(JsonValueKind.Null, detail.GetProperty("title").ValueKind);
        }

        [Fact]
        public async Task Add_NoTitleAndNoNoteAndNoCategory_StoresNullsAndAnswersNulls()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId);

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            JsonElement detail = await _app.ReadExpenseAsync(group.Owner, group.GroupId, expenseId);
            Assert.Equal(JsonValueKind.Null, detail.GetProperty("title").ValueKind);
            Assert.Equal(JsonValueKind.Null, detail.GetProperty("note").ValueKind);
            Assert.Equal(JsonValueKind.Null, detail.GetProperty("categoryId").ValueKind);
        }

        [Fact]
        public async Task Add_BuiltInCategory_IsStoredAndAnswered()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid transport = await _app.BuiltInCategoryIdAsync("transport");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId) with { CategoryId = transport };

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            Assert.Equal(transport.ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "category_id"));
        }

        [Fact]
        public async Task Add_TitleOf80Characters_IsAccepted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            string title = ExpenseInputs.TextOfLength(80);
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId) with { Title = title };

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            Assert.Equal(title, await ExpenseRows.ExpenseValueAsync(_app, expenseId, "title"));
        }

        [Fact]
        public async Task Add_TitleOf80CharactersPlusSurroundingSpaces_IsAcceptedBecauseTheLengthIsAfterTrimming()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            string title = ExpenseInputs.TextOfLength(80);
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId) with { Title = $"  {title}  " };

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            Assert.Equal(title, await ExpenseRows.ExpenseValueAsync(_app, expenseId, "title"));
        }

        [Fact]
        public async Task Add_NoteOf500Characters_IsAccepted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            string note = ExpenseInputs.TextOfLength(500);
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId) with { Note = note };

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            Assert.Equal(note, await ExpenseRows.ExpenseValueAsync(_app, expenseId, "note"));
        }

        [Fact]
        public async Task Add_AnyMemberWhoIsNotTheOwner_CanAddAndBecomesTheCreator()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.MemberRowId, group.OwnerRowId, group.MemberRowId);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Member.Client, group.GroupId, input);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(group.Member.UserId, body.GetProperty("createdByUserId").GetGuid());
            Assert.Equal(GroupsApp.MemberName, body.GetProperty("createdByName").GetString());
            Assert.True(body.GetProperty("canEdit").GetBoolean());
        }

        [Fact]
        public async Task Add_PayerWhoIsNotTheCaller_IsAllowed()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.PlainRowIds[0], group.OwnerRowId, group.PlainRowIds[0]);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Member.Client, group.GroupId, input);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal("Marko", body.GetProperty("paidByName").GetString());
        }

        [Fact]
        public async Task Add_EurExpenseInAMkdGroup_IsAccepted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(1_550, ExpenseInputs.Eur, group.OwnerRowId, group.OwnerRowId, group.MemberRowId);

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            Assert.Equal("EUR", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "currency"));
            Assert.Equal("1550", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "amount_minor"));
        }

        [Fact]
        public async Task Add_FutureDate_IsAccepted()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            string december = _app.Clock.UtcToday.AddDays(60).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId) with { ExpenseDate = december };

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            Assert.Equal(december, await ExpenseRows.ExpenseValueAsync(_app, expenseId, "expense_date"));
        }

        [Fact]
        public async Task Add_TwoExpenses_BothAreSavedWithDifferentIds()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId);

            Guid first = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            Guid second = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            Assert.NotEqual(first, second);
            Assert.Equal(2, await ExpenseRows.CountExpensesAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Add_ValidRequest_WritesOneExpenseAddedEventByTheAdderAboutTheExpense()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId) with { Title = "Dinner" };

            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, input);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ExpenseAdded"));
            Assert.Equal(group.Member.UserId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "ExpenseAdded", "actor_user_id"));
            Assert.Equal(expenseId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "ExpenseAdded", "expense_id"));
            Assert.Null(await GroupRows.EventValueAsync(_app, group.GroupId, "ExpenseAdded", "changes"));
        }

        [Fact]
        public async Task Add_ValidRequest_WritesTheTitleAmountAndCurrencyAsTheEventData()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(1_550, ExpenseInputs.Eur, group.OwnerRowId, group.OwnerRowId) with { Title = "Dinner" };

            await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            Assert.Equal("Dinner", await GroupRows.EventValueAsync(_app, group.GroupId, "ExpenseAdded", "data->>'title'"));
            Assert.Equal("1550", await GroupRows.EventValueAsync(_app, group.GroupId, "ExpenseAdded", "data->>'amountMinor'"));
            Assert.Equal("EUR", await GroupRows.EventValueAsync(_app, group.GroupId, "ExpenseAdded", "data->>'currency'"));
        }

        [Fact]
        public async Task Add_ValidRequest_WritesOneExpenseAddedUsageEventForTheAdder()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId);

            await _app.AddExpenseAsync(group.Member, group.GroupId, input);

            Assert.Equal(1, await GroupRows.CountUsageEventsAsync(_app, group.Member.UserId, "ExpenseAdded"));
            Assert.Equal(0, await GroupRows.CountUsageEventsAsync(_app, group.Owner.UserId, "ExpenseAdded"));
        }

        [Fact]
        public async Task Add_ValidRequest_WritesNoOtherEventThanTheExpenseAddedOne()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId);
            int eventsBefore = await GroupRows.CountEventsAsync(_app, group.GroupId);

            await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            Assert.Equal(eventsBefore + 1, await GroupRows.CountEventsAsync(_app, group.GroupId));
        }
    }
}
