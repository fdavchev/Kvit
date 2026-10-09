using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class OneBillNameTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string ReceiptEmoji = "\U0001F9FE";

        [Fact]
        public async Task OneBill_WithoutATitle_NamesTheGroupBillMiddleDotAndTheExpenseDate()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, input with { Title = null, ExpenseDate = "2026-10-05" });

            Assert.Equal("Bill · 5 Oct", await GroupRows.GroupValueAsync(_app, groupId, "name"));
        }

        [Fact]
        public async Task OneBill_WithoutATitleOnADateWithATwoDigitDay_NamesTheGroupWithThatDayAndMonth()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, input with { Title = null, ExpenseDate = "2026-12-25" });

            Assert.Equal("Bill · 25 Dec", await GroupRows.GroupValueAsync(_app, groupId, "name"));
        }

        [Fact]
        public async Task OneBill_WithABlankTitle_NamesTheGroupLikeWithoutATitle()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, input with { Title = "   ", ExpenseDate = "2026-03-01" });

            Assert.Equal("Bill · 1 Mar", await GroupRows.GroupValueAsync(_app, groupId, "name"));
        }

        [Fact]
        public async Task OneBill_WithoutATitle_UsesTheGeneratedNameInTheGroupCreatedEvent()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, input);

            Assert.Equal("Bill · 5 Oct", await GroupRows.EventValueAsync(_app, groupId, "GroupCreated", "data->>'name'"));
        }

        [Fact]
        public async Task OneBill_WithATitle_NamesTheGroupWithTheTrimmedTitle()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            (Guid groupId, Guid expenseId) = await _app.CreateOneBillAsync(caller, input with { Title = "  Dinner  " });

            Assert.Equal("Dinner", await GroupRows.GroupValueAsync(_app, groupId, "name"));
            Assert.Equal("Dinner", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "title"));
        }

        [Fact]
        public async Task OneBill_TitleOf60Characters_IsAcceptedAsTheGroupName()
        {
            SignedInUser caller = await _app.SignUpAsync();
            string title = ExpenseInputs.TextOfLength(60);
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, input with { Title = title });

            Assert.Equal(title, await GroupRows.GroupValueAsync(_app, groupId, "name"));
        }

        [Theory]
        [InlineData(61)]
        [InlineData(81)]
        public async Task OneBill_TitleLongerThan60Characters_Answers400TitleInvalidAndWritesNothing(int length)
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            HttpResponseMessage response = await ExpenseRequests.OneBillAsync(caller.Client, input with { Title = ExpenseInputs.TextOfLength(length) });

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_TITLE_INVALID);
            await _app.AssertNothingWrittenForAsync(caller);
        }

        [Fact]
        public async Task OneBill_WithoutAnEmoji_UsesTheReceiptEmoji()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, input with { Emoji = null });

            Assert.Equal(ReceiptEmoji, await GroupRows.GroupValueAsync(_app, groupId, "emoji"));
        }

        [Fact]
        public async Task OneBill_WithAnEmoji_StoresThatEmoji()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, input with { Emoji = "\U0001F37D" });

            Assert.Equal("\U0001F37D", await GroupRows.GroupValueAsync(_app, groupId, "emoji"));
        }

        [Fact]
        public async Task OneBill_NameOf60Characters_IsAccepted()
        {
            SignedInUser caller = await _app.SignUpAsync();
            string name = ExpenseInputs.TextOfLength(60);
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, name);

            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, input);

            Assert.Equal(name, (await GroupRequests.ReadMembersAsync(caller.Client, groupId)).GetProperty("members")[1].GetProperty("name").GetString());
        }

        [Fact]
        public async Task OneBill_NameWithSurroundingSpaces_IsStoredTrimmed()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "  Marko  ");

            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, input);

            List<string?> plainNames = await _app.QueryAsync(
                "SELECT name FROM group_members WHERE group_id = @group_id AND user_id IS NULL",
                new NpgsqlParameter("group_id", groupId));
            Assert.Equal("Marko", Assert.Single(plainNames));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task OneBill_EmptyOrBlankName_Answers400MemberNameInvalidAndWritesNothing(string name)
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko", name);

            HttpResponseMessage response = await ExpenseRequests.OneBillAsync(caller.Client, input);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_INVALID);
            await _app.AssertNothingWrittenForAsync(caller);
        }

        [Fact]
        public async Task OneBill_NameOf61Characters_Answers400MemberNameInvalidAndWritesNothing()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, ExpenseInputs.TextOfLength(61));

            HttpResponseMessage response = await ExpenseRequests.OneBillAsync(caller.Client, input);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_INVALID);
            await _app.AssertNothingWrittenForAsync(caller);
        }

        [Theory]
        [InlineData("Marko", "Marko")]
        [InlineData("Marko", "marko")]
        [InlineData("Marko", "MARKO")]
        [InlineData("Marko", "  marko ")]
        public async Task OneBill_NameRepeatedWithAnyLetterCase_Answers400MemberNameTakenAndWritesNothing(string first, string second)
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, first, second);

            HttpResponseMessage response = await ExpenseRequests.OneBillAsync(caller.Client, input);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_TAKEN);
            await _app.AssertNothingWrittenForAsync(caller);
        }

        [Theory]
        [InlineData("Ana")]
        [InlineData("ana")]
        [InlineData("ANA")]
        [InlineData(" Ana ")]
        public async Task OneBill_NameEqualToTheCallersAccountName_Answers400MemberNameTakenAndWritesNothing(string name)
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko", name);

            HttpResponseMessage response = await ExpenseRequests.OneBillAsync(caller.Client, input);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_TAKEN);
            await _app.AssertNothingWrittenForAsync(caller);
        }

        [Fact]
        public async Task OneBill_NameThatSomeoneElseUsesInAnotherGroup_IsAccepted()
        {
            SignedInUser first = await _app.SignUpAsync();
            SignedInUser second = await _app.SignUpAsync("Bojan");
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");
            await _app.CreateOneBillAsync(first, input);

            HttpResponseMessage response = await ExpenseRequests.OneBillAsync(second.Client, input);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
