using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class EditExpenseVersionTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string EditedEvent = "ExpenseEdited";
        private const string NewTitle = "Dinner";
        private const string TitleAndAmount = "coalesce(title, '') || '|' || amount_minor";

        [Fact]
        public async Task Get_AfterAnEdit_AnswersANewVersion()
        {
            (ExpenseGroup group, ExpenseInput original, Guid expenseId) = await AddExpenseAsync();
            uint before = await VersionAsync(group, expenseId);

            Assert.Equal(HttpStatusCode.NoContent, (await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, original with { Title = NewTitle })).StatusCode);

            Assert.NotEqual(before, await VersionAsync(group, expenseId));
        }

        [Fact]
        public async Task Edit_WithTheVersionTheFormWasOpenedFrom_Answers204AndSaves()
        {
            (ExpenseGroup group, ExpenseInput original, Guid expenseId) = await AddExpenseAsync();
            uint opened = await VersionAsync(group, expenseId);

            HttpResponseMessage response = await ExpenseRequests.EditFromVersionAsync(group.Member.Client, group.GroupId, expenseId, original with { Title = NewTitle }, opened);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal($"{NewTitle}|{ExpenseInputs.OneThousandMkd}", await ExpenseRows.ExpenseValueAsync(_app, expenseId, TitleAndAmount));
        }

        [Fact]
        public async Task Edit_FromAVersionOpenedBeforeSomeoneElseSaved_Answers409ChangedMeanwhileAndKeepsTheOtherSave()
        {
            (ExpenseGroup group, ExpenseInput original, Guid expenseId) = await AddExpenseAsync();
            uint opened = await VersionAsync(group, expenseId);
            Assert.Equal(HttpStatusCode.NoContent, (await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, original with { AmountMinor = ExpenseInputs.TwelveHundredMkd })).StatusCode);

            HttpResponseMessage response = await ExpenseRequests.EditFromVersionAsync(group.Member.Client, group.GroupId, expenseId, original with { Title = NewTitle }, opened);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Conflict, ResultCodes.EXPENSE_CHANGED_MEANWHILE);
            Assert.Equal($"|{ExpenseInputs.TwelveHundredMkd}", await ExpenseRows.ExpenseValueAsync(_app, expenseId, TitleAndAmount));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, EditedEvent));
        }

        [Fact]
        public async Task Edit_FromTheNewVersionAfterARefusal_Answers204AndSaves()
        {
            (ExpenseGroup group, ExpenseInput original, Guid expenseId) = await AddExpenseAsync();
            uint opened = await VersionAsync(group, expenseId);
            ExpenseInput newAmount = original with { AmountMinor = ExpenseInputs.TwelveHundredMkd };
            Assert.Equal(HttpStatusCode.NoContent, (await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, newAmount)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await ExpenseRequests.EditFromVersionAsync(group.Member.Client, group.GroupId, expenseId, original with { Title = NewTitle }, opened)).StatusCode);

            HttpResponseMessage response = await ExpenseRequests.EditFromVersionAsync(group.Member.Client, group.GroupId, expenseId, newAmount with { Title = NewTitle }, await VersionAsync(group, expenseId));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal($"{NewTitle}|{ExpenseInputs.TwelveHundredMkd}", await ExpenseRows.ExpenseValueAsync(_app, expenseId, TitleAndAmount));
        }

        [Fact]
        public async Task Edit_WithoutAVersionAfterSomeoneElseSaved_SavesTheWholeFormAsBefore()
        {
            (ExpenseGroup group, ExpenseInput original, Guid expenseId) = await AddExpenseAsync();
            Assert.Equal(HttpStatusCode.NoContent, (await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, original with { AmountMinor = ExpenseInputs.TwelveHundredMkd })).StatusCode);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Member.Client, group.GroupId, expenseId, original with { Title = NewTitle });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal($"{NewTitle}|{ExpenseInputs.OneThousandMkd}", await ExpenseRows.ExpenseValueAsync(_app, expenseId, TitleAndAmount));
            Assert.Equal(2, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, EditedEvent));
        }

        [Fact]
        public async Task Edit_ChangingTheCurrencyFromAVersionOpenedBeforeSomeoneElseSavedWhileTheRateIsStale_Answers409AndDoesNotAskTheSource()
        {
            (ExpenseGroup group, ExpenseInput original, Guid expenseId) = await AddExpenseAsync();
            uint opened = await VersionAsync(group, expenseId);
            Assert.Equal(HttpStatusCode.NoContent, (await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, original with { Title = NewTitle })).StatusCode);
            await _app.PrepareRatesAsync(new DateOnly(2026, 10, 3), 62.2500m, TimeSpan.FromHours(20));

            HttpResponseMessage response = await ExpenseRequests.EditFromVersionAsync(group.Member.Client, group.GroupId, expenseId, original with { Currency = ExpenseInputs.Eur }, opened);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Conflict, ResultCodes.EXPENSE_CHANGED_MEANWHILE);
            Assert.Empty(_app.RateSource.RequestedDates);
            Assert.Equal(ExpenseInputs.Mkd, await ExpenseRows.ExpenseValueAsync(_app, expenseId, "currency"));
        }

        private async Task<(ExpenseGroup Group, ExpenseInput Original, Guid ExpenseId)> AddExpenseAsync()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput original = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, original);

            return (group, original, expenseId);
        }

        private async Task<uint> VersionAsync(ExpenseGroup group, Guid expenseId)
        {
            return (await _app.ReadExpenseAsync(group.Member, group.GroupId, expenseId)).GetProperty("version").GetUInt32();
        }
    }
}
