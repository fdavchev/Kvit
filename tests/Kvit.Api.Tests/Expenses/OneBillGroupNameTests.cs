using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class OneBillGroupNameTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string EnglishBillName = "Bill · 7 Oct";
        private const string MacedonianBillName = "Сметка · 7 окт";

        [Theory]
        [InlineData(EnglishBillName)]
        [InlineData(MacedonianBillName)]
        public async Task OneBill_WithoutATitleAndWithAGroupName_NamesTheGroupWithTheGroupNameAsSent(string groupName)
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, input with { Title = null, GroupName = groupName });

            Assert.Equal(groupName, await GroupRows.GroupValueAsync(_app, groupId, "name"));
        }

        [Fact]
        public async Task OneBill_WithoutATitleAndWithAGroupName_KeepsTheExpenseUntitled()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            (_, Guid expenseId) = await _app.CreateOneBillAsync(caller, input with { Title = null, GroupName = EnglishBillName });

            Assert.Null(await ExpenseRows.ExpenseValueAsync(_app, expenseId, "title"));
        }

        [Fact]
        public async Task OneBill_WithoutATitleAndWithAGroupName_UsesTheGroupNameInTheGroupCreatedEvent()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, input with { Title = null, GroupName = MacedonianBillName });

            Assert.Equal(MacedonianBillName, await GroupRows.EventValueAsync(_app, groupId, "GroupCreated", "data->>'name'"));
        }

        [Fact]
        public async Task OneBill_WithABlankTitleAndAGroupName_NamesTheGroupWithTheGroupName()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            (Guid groupId, Guid expenseId) = await _app.CreateOneBillAsync(caller, input with { Title = "   ", GroupName = MacedonianBillName });

            Assert.Equal(MacedonianBillName, await GroupRows.GroupValueAsync(_app, groupId, "name"));
            Assert.Null(await ExpenseRows.ExpenseValueAsync(_app, expenseId, "title"));
        }

        [Fact]
        public async Task OneBill_WithAGroupNameOfAnotherDateThanTheExpense_KeepsTheGroupNameAsSent()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, input with { Title = null, ExpenseDate = "2026-12-25", GroupName = EnglishBillName });

            Assert.Equal(EnglishBillName, await GroupRows.GroupValueAsync(_app, groupId, "name"));
        }

        [Fact]
        public async Task OneBill_WithATitleAndAGroupName_TheTitleWinsForTheGroupAndTheExpense()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            (Guid groupId, Guid expenseId) = await _app.CreateOneBillAsync(caller, input with { Title = "  Dinner  ", GroupName = EnglishBillName });

            Assert.Equal("Dinner", await GroupRows.GroupValueAsync(_app, groupId, "name"));
            Assert.Equal("Dinner", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "title"));
        }

        [Fact]
        public async Task OneBill_WithATitleAndAGroupNameOver60Characters_IgnoresTheGroupNameAndAccepts()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, input with { Title = "Dinner", GroupName = ExpenseInputs.TextOfLength(61) });

            Assert.Equal("Dinner", await GroupRows.GroupValueAsync(_app, groupId, "name"));
        }

        [Fact]
        public async Task OneBill_WithAGroupNameWithSurroundingSpaces_NamesTheGroupWithTheTrimmedGroupName()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, input with { GroupName = $"  {EnglishBillName}  " });

            Assert.Equal(EnglishBillName, await GroupRows.GroupValueAsync(_app, groupId, "name"));
        }

        [Fact]
        public async Task OneBill_GroupNameOf60Characters_IsAcceptedAsTheGroupName()
        {
            SignedInUser caller = await _app.SignUpAsync();
            string groupName = ExpenseInputs.TextOfLength(60);
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, input with { GroupName = groupName });

            Assert.Equal(groupName, await GroupRows.GroupValueAsync(_app, groupId, "name"));
        }

        [Fact]
        public async Task OneBill_GroupNameOf60CharactersWithSurroundingSpaces_IsAcceptedBecauseTheLengthCountsAfterTrimming()
        {
            SignedInUser caller = await _app.SignUpAsync();
            string groupName = ExpenseInputs.TextOfLength(60);
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, input with { GroupName = $"  {groupName}  " });

            Assert.Equal(groupName, await GroupRows.GroupValueAsync(_app, groupId, "name"));
        }

        [Theory]
        [InlineData(61)]
        [InlineData(81)]
        public async Task OneBill_GroupNameLongerThan60CharactersWithoutATitle_Answers400GroupNameInvalidAndWritesNothing(int length)
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            HttpResponseMessage response = await ExpenseRequests.OneBillAsync(caller.Client, input with { Title = null, GroupName = ExpenseInputs.TextOfLength(length) });

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.GROUP_NAME_INVALID);
            await _app.AssertNothingWrittenForAsync(caller);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task OneBill_BlankGroupNameWithoutATitle_IsTreatedAsAbsentAndNamesTheGroupLikeTheServerDefault(string groupName)
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, input with { Title = null, ExpenseDate = "2026-10-05", GroupName = groupName });

            Assert.Equal("Bill · 5 Oct", await GroupRows.GroupValueAsync(_app, groupId, "name"));
        }

        [Fact]
        public async Task OneBill_WithoutATitleAndWithoutAGroupName_StillNamesTheGroupWithTheServerDefault()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, input with { Title = null, ExpenseDate = "2026-12-25", GroupName = null });

            Assert.Equal("Bill · 25 Dec", await GroupRows.GroupValueAsync(_app, groupId, "name"));
        }
    }
}
