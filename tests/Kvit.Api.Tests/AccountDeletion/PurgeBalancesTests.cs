using System.Text.Json;
using Kvit.Api.Tests.Balances;
using Kvit.Api.Tests.BudgetCurrency;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public class PurgeBalancesTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [InlineData("KeepName")]
        [InlineData("FormerMember")]
        public async Task Purge_BalancesPerMemberId_AreTheSameBeforeAndAfter(string nameChoice)
        {
            ExpenseGroup group = await CreateGroupWithMoneyMovingAsync();
            List<string> before = await DeletionSteps.BalancesAsync(group.Owner.Client, group.GroupId);

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, nameChoice);

            Assert.NotEmpty(before);
            Assert.Equal(before, await DeletionSteps.BalancesAsync(group.Owner.Client, group.GroupId));
        }

        [Fact]
        public async Task Purge_TheBalanceRowOfThePerson_KeepsItsMemberIdAndIsStillCurrent()
        {
            ExpenseGroup group = await CreateGroupWithMoneyMovingAsync();

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.FormerMember);

            JsonElement body = await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId);
            JsonElement mkd = BalanceJson.CurrencyOf(body, ExpenseInputs.Mkd);
            Assert.Contains(group.MemberRowId, BalanceJson.BalanceMemberIds(mkd));
            int index = BalanceJson.BalanceMemberIds(mkd).IndexOf(group.MemberRowId);
            Assert.True(BalanceJson.BalanceIsCurrents(mkd)[index]);
            Assert.Equal(DeletionRequests.FormerMemberEnglish, BalanceJson.BalanceNames(mkd)[index]);
        }

        [Fact]
        public async Task Purge_SharesAndPayerOfTheExpenses_PointAtTheSameMemberRows()
        {
            ExpenseGroup group = await CreateGroupWithMoneyMovingAsync();
            string sharesBefore = await DeletionRows.SnapshotOfTablesAsync(_app, "expense_shares", "settlements");

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.FormerMember);

            Assert.Equal(sharesBefore, await DeletionRows.SnapshotOfTablesAsync(_app, "expense_shares", "settlements"));
        }

        [Fact]
        public async Task Purge_ExpenseDetailPaidByThePerson_ShowsThePayerRowWithTheNewName()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await BudgetCurrencySteps.UseRateAsync(_app, PersonalDataSteps.MkdPerEur);
            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, ExpenseInputs.Equal(120_000, ExpenseInputs.Mkd, group.MemberRowId, group.OwnerRowId, group.MemberRowId));

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.FormerMember);

            JsonElement detail = await _app.ReadExpenseAsync(group.Owner, group.GroupId, expenseId);
            Assert.Equal(group.MemberRowId, detail.GetProperty("paidByMemberId").GetGuid());
            Assert.Equal(DeletionRequests.FormerMemberEnglish, detail.GetProperty("paidByName").GetString());
        }

        [Fact]
        public async Task Purge_OwnerWhoPaidAndOwes_BalancesStayTheSameWhileOwnershipMoves()
        {
            ExpenseGroup group = await CreateGroupWithMoneyMovingAsync();
            List<string> before = await DeletionSteps.BalancesAsync(group.Member.Client, group.GroupId);

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Owner.Client, group.Owner.UserId, DeletionRequests.KeepName);

            Assert.Equal(before, await DeletionSteps.BalancesAsync(group.Member.Client, group.GroupId));
        }

        private async Task<ExpenseGroup> CreateGroupWithMoneyMovingAsync()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await BudgetCurrencySteps.UseRateAsync(_app, PersonalDataSteps.MkdPerEur);
            await _app.AddExpenseAsync(group.Member, group.GroupId, ExpenseInputs.Equal(120_000, ExpenseInputs.Mkd, group.MemberRowId, group.OwnerRowId, group.MemberRowId, group.PlainRowIds[0]));
            await _app.AddExpenseAsync(group.Owner, group.GroupId, ExpenseInputs.Equal(3_000, ExpenseInputs.Eur, group.OwnerRowId, group.OwnerRowId, group.MemberRowId));
            Guid payment = await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.OwnerRowId, group.MemberRowId, 5_000, ExpenseInputs.Mkd);
            await SettlementSteps.ConfirmAsync(group.Member, group.GroupId, payment);

            return group;
        }
    }
}
