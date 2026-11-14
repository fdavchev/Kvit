using System.Net;
using Kvit.Api.Tests.BudgetCurrency;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public static class DeletedPersonSteps
    {
        public const string OldName = "Zorica Petrovska";
        public const string DinnerTitle = "Dinner";

        public static async Task<DeletedPersonScenario> CreateAsync(ExpensesApp app, string nameChoice)
        {
            app.Clock.SetTo(ExpensesApp.ClockStart);
            await BudgetCurrencySteps.UseRateAsync(app, PersonalDataSteps.MkdPerEur);
            ExpenseGroup group = await app.CreateExpenseGroupAsync("Marko");
            SignedInUser leaver = group.Member;
            await UserRows.SetDisplayNameAsync(app, leaver.UserId, OldName);

            await SettlementSteps.AssertNoContentAsync(await GroupRequests.MakeOwnerAsync(group.Owner.Client, group.GroupId, group.MemberRowId));
            await SettlementSteps.AssertNoContentAsync(await GroupRequests.MakeOwnerAsync(leaver.Client, group.GroupId, group.OwnerRowId));
            ExpenseInput dinner = ExpenseInputs.Equal(120_000, ExpenseInputs.Mkd, group.MemberRowId, group.OwnerRowId, group.MemberRowId, group.PlainRowIds[0]) with { Title = DinnerTitle };
            Guid expenseId = await app.AddExpenseAsync(leaver, group.GroupId, dinner);
            ExpenseInput edited = ExpenseInputs.Equal(120_000, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId) with { Title = DinnerTitle };
            await SettlementSteps.AssertNoContentAsync(await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, edited));
            Guid settlementId = await SettlementSteps.RecordAsync(leaver, group.GroupId, group.MemberRowId, group.OwnerRowId, 10_000, ExpenseInputs.Mkd);

            await DeletionSteps.RequestAndPurgeAsync(app, leaver.Client, leaver.UserId, nameChoice);

            return new DeletedPersonScenario(group, leaver, expenseId, settlementId, OldName);
        }

        public static async Task<string> AnswerTextOfAsync(DeletedPersonScenario scenario, string answer)
        {
            HttpClient viewer = scenario.Group.Owner.Client;
            Guid groupId = scenario.Group.GroupId;
            string path = answer switch
            {
                "groups" => GroupRequests.GroupsPath,
                "group" => GroupRequests.PathOf(groupId),
                "members" => GroupRequests.MembersPathOf(groupId),
                "expenses" => ExpenseRequests.ExpensesPathOf(groupId),
                "expense" => ExpenseRequests.ExpensePathOf(groupId, scenario.ExpenseId),
                "deleted-expenses" => ExpenseRequests.DeletedPathOf(groupId),
                "activity" => ExpenseRequests.ActivityPathOf(groupId),
                "balances" => $"/api/groups/{groupId}/balances",
                "settlements" => $"/api/groups/{groupId}/settlements",
                "dashboard" => "/api/dashboard",
                _ => throw new ArgumentOutOfRangeException(nameof(answer), answer, "Not one of the answers this scenario reads."),
            };

            HttpResponseMessage response = await viewer.GetAsync(path, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }
    }
}
