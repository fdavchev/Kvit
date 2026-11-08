using Kvit.Api.Tests.BudgetCurrency;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public class PurgePersonalDataTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Purge_PersonalBudget_IsDeletedWithoutAForeignKeyError()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid expenseId = await DeletionGroups.CreateNeutralExpenseAsync(_app);
            await PersonalDataSteps.CreateFullSetAsync(_app, user, expenseId);
            Assert.Equal(PersonalCounts.Full, await DeletionRows.PersonalCountsAsync(_app, user.UserId));

            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            Assert.Equal(PersonalCounts.Empty, await DeletionRows.PersonalCountsAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Purge_FormerMember_AlsoDeletesThePersonalBudget()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid expenseId = await DeletionGroups.CreateNeutralExpenseAsync(_app);
            await PersonalDataSteps.CreateFullSetAsync(_app, user, expenseId);

            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.FormerMember);

            Assert.Equal(PersonalCounts.Empty, await DeletionRows.PersonalCountsAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Purge_PersonalEntriesIncludingDeletedAndGeneratedOnes_AreAllGone()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid expenseId = await DeletionGroups.CreateNeutralExpenseAsync(_app);
            await PersonalDataSteps.CreateFullSetAsync(_app, user, expenseId);

            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            Assert.Equal(0, await DeletionRows.CountOfUserAsync(_app, "personal_entries", "user_id", user.UserId));
            Assert.Equal(0, await DeletionRows.CountOfUserAsync(_app, "recurring_incomes", "user_id", user.UserId));
        }

        [Fact]
        public async Task Purge_CustomCategoriesIncludingArchivedOnes_AreGone()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid expenseId = await DeletionGroups.CreateNeutralExpenseAsync(_app);
            await PersonalDataSteps.CreateFullSetAsync(_app, user, expenseId);

            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            Assert.Equal(0, await DeletionRows.CountOfUserAsync(_app, "categories", "owner_user_id", user.UserId));
        }

        [Fact]
        public async Task Purge_BuiltInCategories_StayForEveryone()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid expenseId = await DeletionGroups.CreateNeutralExpenseAsync(_app);
            await PersonalDataSteps.CreateFullSetAsync(_app, user, expenseId);
            string? builtInBefore = Assert.Single(await _app.QueryAsync("SELECT count(*)::text FROM categories WHERE owner_user_id IS NULL"));

            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            Assert.NotEqual("0", builtInBefore);
            Assert.Equal(builtInBefore, Assert.Single(await _app.QueryAsync("SELECT count(*)::text FROM categories WHERE owner_user_id IS NULL")));
        }

        [Fact]
        public async Task Purge_BudgetLimitsAndDuplicateAnswers_AreGone()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid expenseId = await DeletionGroups.CreateNeutralExpenseAsync(_app);
            await PersonalDataSteps.CreateFullSetAsync(_app, user, expenseId);

            await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            Assert.Equal(0, await BudgetLimitRows.CountOfUserAsync(_app, user.UserId));
            Assert.Equal(0, await DuplicateAnswerRows.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Purge_ASecondUsersIdenticalPersonalData_StaysByteForByte()
        {
            SignedInUser leaver = await _app.SignUpAsync("Zorica");
            SignedInUser other = await _app.SignUpAsync("Bojan");
            Guid expenseId = await DeletionGroups.CreateNeutralExpenseAsync(_app);
            await PersonalDataSteps.CreateFullSetAsync(_app, leaver, expenseId);
            await PersonalDataSteps.CreateFullSetAsync(_app, other, expenseId);
            string otherBefore = await DeletionRows.SnapshotOfRowsOfUserAsync(_app, other.UserId);

            await DeletionSteps.RequestAndPurgeAsync(_app, leaver.Client, leaver.UserId, DeletionRequests.FormerMember);

            Assert.Equal(PersonalCounts.Full, await DeletionRows.PersonalCountsAsync(_app, other.UserId));
            Assert.Equal(otherBefore, await DeletionRows.SnapshotOfRowsOfUserAsync(_app, other.UserId));
        }

        [Fact]
        public async Task Purge_UserWithNoPersonalDataAtAll_Succeeds()
        {
            SignedInUser user = await _app.SignUpAsync();

            DateTimeOffset now = await DeletionSteps.RequestAndPurgeAsync(_app, user.Client, user.UserId, DeletionRequests.KeepName);

            Assert.Equal(now, (await DeletionRows.ReadAsync(_app, user.UserId)).PurgedAt);
        }

        [Fact]
        public async Task Purge_ExpensesSharesPaymentsAndClosingAnswers_AreByteIdentical()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            await BudgetCurrencySteps.UseRateAsync(_app, PersonalDataSteps.MkdPerEur);
            ExpenseInput dinner = ExpenseInputs.Equal(120_000, ExpenseInputs.Mkd, group.MemberRowId, group.OwnerRowId, group.MemberRowId, group.PlainRowIds[0]);
            await _app.AddExpenseAsync(group.Member, group.GroupId, dinner);
            await SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.OwnerRowId, group.MemberRowId, 10_000, ExpenseInputs.Mkd);
            string before = await DeletionRows.SnapshotOfTheTablesAPurgeMustNotTouchAsync(_app);

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.FormerMember);

            Assert.Equal(before, await DeletionRows.SnapshotOfTheTablesAPurgeMustNotTouchAsync(_app));
        }

        [Fact]
        public async Task Purge_ExpenseTitleAndNoteEqualToThePersonsName_AreNotTouched()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await UserRows.SetDisplayNameAsync(_app, group.Member.UserId, "Zorica Petrovska");
            Guid expenseId = await ExpenseRows.InsertExpenseAsync(_app, group.GroupId, group.MemberRowId, group.Member.UserId, title: "Zorica Petrovska", note: "Zorica Petrovska");

            await DeletionSteps.RequestAndPurgeAsync(_app, group.Member.Client, group.Member.UserId, DeletionRequests.FormerMember);

            Assert.Equal("Zorica Petrovska", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "title"));
            Assert.Equal("Zorica Petrovska", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "note"));
        }
    }
}
