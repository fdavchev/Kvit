using System.Text.Json;
using Kvit.Api.Tests.AccountDeletion;
using Kvit.Api.Tests.BudgetCurrency;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Export
{
    public class ExportFormerMemberTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string OldName = "Zorica Petrovska";
        private const string LunchTitle = "Lunch by the river";

        [Fact]
        public async Task Export_Activity_RowsOfAPersonWhoChoseFormerMember_ShowFormerMemberAsTheActor()
        {
            (ExpenseGroup group, Guid expenseId, ExportBody body) = await ExportAfterTheMemberIsPurgedAsync(DeletionRequests.FormerMember);

            JsonElement expenseAdded = Assert.Single(ActivityOf(body, group), row => ExportBody.GuidOrNull(row, "expenseId") == expenseId);
            JsonElement paymentRecorded = Assert.Single(ActivityOf(body, group), row => row.GetProperty("type").GetString() == "SettlementRecorded");

            Assert.Equal(DeletionRequests.FormerMemberEnglish, expenseAdded.GetProperty("actorName").GetString());
            Assert.Equal(DeletionRequests.FormerMemberEnglish, paymentRecorded.GetProperty("actorName").GetString());
            Assert.Equal(DeletionRequests.FormerMemberEnglish, paymentRecorded.GetProperty("data").GetProperty("fromName").GetString());
            Assert.Equal(LunchTitle, expenseAdded.GetProperty("data").GetProperty("title").GetString());
        }

        [Fact]
        public async Task Export_Activity_OfAPersonWhoChoseFormerMember_NeverMentionsTheOldName()
        {
            (_, _, ExportBody body) = await ExportAfterTheMemberIsPurgedAsync(DeletionRequests.FormerMember);

            body.AssertDoesNotMention(OldName);
        }

        [Fact]
        public async Task Export_Activity_RowsOfAPersonWhoKeptTheirName_ShowTheirName()
        {
            (ExpenseGroup group, Guid expenseId, ExportBody body) = await ExportAfterTheMemberIsPurgedAsync(DeletionRequests.KeepName);

            JsonElement expenseAdded = Assert.Single(ActivityOf(body, group), row => ExportBody.GuidOrNull(row, "expenseId") == expenseId);

            Assert.Equal(OldName, expenseAdded.GetProperty("actorName").GetString());
        }

        private async Task<(ExpenseGroup Group, Guid ExpenseId, ExportBody Body)> ExportAfterTheMemberIsPurgedAsync(string nameChoice)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await UserRows.SetDisplayNameAsync(_app, group.Member.UserId, OldName);
            await BudgetCurrencySteps.UseRateAsync(_app, PersonalDataSteps.MkdPerEur);
            ExpenseInput lunch = ExpenseInputs.Equal(120_000, ExpenseInputs.Mkd, group.MemberRowId, group.OwnerRowId, group.MemberRowId) with { Title = LunchTitle };
            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, lunch);
            await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.MemberRowId, group.OwnerRowId, 5_000, ExpenseInputs.Mkd);

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, nameChoice);

            return (group, expenseId, await ExportRequests.GetBodyAsync(group.Owner.Client));
        }

        private static List<JsonElement> ActivityOf(ExportBody body, ExpenseGroup group)
        {
            return ExportBody.ListOf(body.GroupWithId(group.GroupId), "activity");
        }
    }
}
