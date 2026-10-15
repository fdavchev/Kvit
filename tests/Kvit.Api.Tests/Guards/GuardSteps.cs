using System.Net;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Guards
{
    public static class GuardSteps
    {
        public const string RenamedGroup = "Renamed trip";
        public const string NewPlainName = "Petre";
        public const long BiggerExpenseMinor = 80_000;

        public static ExpenseInput MemberExpenseInput(SettlementGroup group, long amountMinor = ZeroBalanceScenario.ExpenseMinor)
        {
            return ExpenseInputs.Equal(amountMinor, ExpenseInputs.Mkd, group.MemberRowId, group.MemberRowId, group.OwnerRowId);
        }

        public static ExpenseInput OwnerExpenseInput(SettlementGroup group, long amountMinor = ZeroBalanceScenario.ExpenseMinor)
        {
            return ExpenseInputs.Equal(amountMinor, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
        }

        public static async Task<Guid> AddBalancedPairOfExpensesAsync(ExpensesApp app, SettlementGroup group)
        {
            Guid memberExpenseId = await app.AddExpenseAsync(group.Member, group.GroupId, MemberExpenseInput(group));
            await app.AddExpenseAsync(group.Owner, group.GroupId, OwnerExpenseInput(group));

            return memberExpenseId;
        }

        public static async Task<Guid> AddAndDeleteMemberExpenseAsync(ExpensesApp app, SettlementGroup group)
        {
            Guid expenseId = await app.AddExpenseAsync(group.Member, group.GroupId, MemberExpenseInput(group));
            await SettlementSteps.AssertNoContentAsync(await ExpenseRequests.DeleteAsync(group.Member.Client, group.GroupId, expenseId));

            return expenseId;
        }

        public static async Task<Guid> RecordConfirmedPaymentByMemberAsync(SettlementGroup group)
        {
            return await SettlementSteps.RecordAsync(group.Member, group.GroupId, group.OwnerRowId, group.MemberRowId, ZeroBalanceScenario.ShareMinor);
        }

        public static async Task AssertSucceededAsync(HttpResponseMessage response)
        {
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            Assert.True(response.IsSuccessStatusCode, $"Expected a success answer, got {(int)response.StatusCode} {response.StatusCode}: {body}");
        }

        public static async Task AssertNoContentAsync(Task<HttpResponseMessage> request)
        {
            await SettlementSteps.AssertNoContentAsync(await request);
        }

        public static async Task AssertOkAsync(Task<HttpResponseMessage> request)
        {
            HttpResponseMessage response = await request;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
