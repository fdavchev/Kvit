using Kvit.Api.Tests.AccountDeletion;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Cleanup
{
    public class CleanupBystanderTests(CleanupApp app) : CleanupTestBase(app)
    {
        [Fact]
        public async Task Post_WithRowsDueElsewhere_TheGroupOfABystanderStaysByteForByteTheSame()
        {
            FullGroup bystander = await CleanupSteps.CreateFullGroupAsync(_app);
            await CleanupSteps.AddOneDueRowOfEveryKindAsync(_app);
            string groupBefore = await GroupRows.SnapshotAsync(_app, bystander.Group.GroupId);
            string expensesBefore = await ExpenseRows.SnapshotAsync(_app, bystander.Group.GroupId);
            string settlementsBefore = await SettlementRows.SnapshotAsync(_app, bystander.Group.GroupId);

            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.OneOfEveryKind, result);
            Assert.Equal(groupBefore, await GroupRows.SnapshotAsync(_app, bystander.Group.GroupId));
            Assert.Equal(expensesBefore, await ExpenseRows.SnapshotAsync(_app, bystander.Group.GroupId));
            Assert.Equal(settlementsBefore, await SettlementRows.SnapshotAsync(_app, bystander.Group.GroupId));
        }

        [Fact]
        public async Task Post_WithRowsDueElsewhere_TheWholePersonalBudgetOfABystanderStaysTheSame()
        {
            FullGroup bystander = await CleanupSteps.CreateFullGroupAsync(_app);
            await PersonalDataSteps.CreateFullSetAsync(_app, bystander.Group.Owner, bystander.OwnerExpenseId);
            await CleanupSteps.AddOneDueRowOfEveryKindAsync(_app);
            string before = await DeletionRows.SnapshotOfRowsOfUserAsync(_app, bystander.Group.Owner.UserId);
            PersonalCounts countsBefore = await DeletionRows.PersonalCountsAsync(_app, bystander.Group.Owner.UserId);

            await _app.RunCleanupAsync();

            Assert.Equal(before, await DeletionRows.SnapshotOfRowsOfUserAsync(_app, bystander.Group.Owner.UserId));
            Assert.Equal(countsBefore, await DeletionRows.PersonalCountsAsync(_app, bystander.Group.Owner.UserId));
        }

        [Fact]
        public async Task Post_WithRowsDueElsewhere_TablesTheCleanupHasNoBusinessWithStayTheSame()
        {
            FullGroup bystander = await CleanupSteps.CreateFullGroupAsync(_app);
            await PersonalDataSteps.CreateFullSetAsync(_app, bystander.Group.Owner, bystander.OwnerExpenseId);
            await CleanupSteps.AddOneDueRowOfEveryKindAsync(_app);
            string[] tables = ["categories", "budget_limits", "recurring_incomes", "exchange_rates", "user_logins", "user_roles", "roles"];
            string before = await DeletionRows.SnapshotOfTablesAsync(_app, tables);

            await _app.RunCleanupAsync();

            Assert.Equal(before, await DeletionRows.SnapshotOfTablesAsync(_app, tables));
        }

        [Fact]
        public async Task Post_WithRowsDueElsewhere_ABystanderBalancesStayTheSame()
        {
            FullGroup bystander = await CleanupSteps.CreateFullGroupAsync(_app);
            await CleanupSteps.AddOneDueRowOfEveryKindAsync(_app);
            List<string> before = await DeletionSteps.BalancesAsync(bystander.Group.Owner.Client, bystander.Group.GroupId);
            Assert.NotEmpty(before);

            await _app.RunCleanupAsync();

            Assert.Equal(before, await DeletionSteps.BalancesAsync(bystander.Group.Owner.Client, bystander.Group.GroupId));
        }
    }
}
