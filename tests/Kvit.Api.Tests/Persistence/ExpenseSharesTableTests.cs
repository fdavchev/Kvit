using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class ExpenseSharesTableTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        [Theory]
        [InlineData("expense_id", "uuid")]
        [InlineData("member_id", "uuid")]
        [InlineData("input_value", "bigint")]
        [InlineData("share_minor", "bigint")]
        public async Task Column_HasTheExpectedType(string column, string expectedDataType)
        {
            string? dataType = await SchemaQueries.DataTypeAsync(_database, "expense_shares", column);

            Assert.Equal(expectedDataType, dataType);
        }

        [Theory]
        [InlineData("expense_id")]
        [InlineData("member_id")]
        [InlineData("input_value")]
        [InlineData("share_minor")]
        public async Task Column_IsNotNullable(string column)
        {
            string? isNullable = await SchemaQueries.IsNullableAsync(_database, "expense_shares", column);

            Assert.Equal("NO", isNullable);
        }

        [Fact]
        public async Task Table_HasExactlyTheFourColumns()
        {
            List<string?> columns = await SchemaQueries.ColumnNamesAsync(_database, "expense_shares");

            Assert.Equal(["expense_id", "input_value", "member_id", "share_minor"], columns);
        }

        [Fact]
        public async Task PrimaryKey_IsExpenseIdTogetherWithMemberId()
        {
            List<string?> primaryKeyColumns = await _database.QueryAsync(
                "SELECT a.attname FROM pg_index i JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY (i.indkey) "
                + "WHERE i.indrelid = 'expense_shares'::regclass AND i.indisprimary ORDER BY a.attname");

            Assert.Equal(["expense_id", "member_id"], primaryKeyColumns);
        }

        [Fact]
        public async Task Share_ForAnExpenseAndAMember_IsAccepted()
        {
            (Guid expenseId, Guid memberId) = await NewExpenseAsync();

            await ExpenseRows.InsertShareAsync(_database, expenseId, memberId, 5_000, 50_000);

            List<StoredShare> shares = await ExpenseRows.SharesOfAsync(_database, expenseId);
            StoredShare share = Assert.Single(shares);
            Assert.Equal(5_000, share.InputValue);
            Assert.Equal(50_000, share.ShareMinor);
        }

        [Fact]
        public async Task Share_ForTheSameExpenseAndMemberTwice_ViolatesThePrimaryKey()
        {
            (Guid expenseId, Guid memberId) = await NewExpenseAsync();
            await ExpenseRows.InsertShareAsync(_database, expenseId, memberId);

            await RejectedInsert.AssertAsync(
                () => ExpenseRows.InsertShareAsync(_database, expenseId, memberId),
                PostgresErrorCodes.UniqueViolation);
        }

        [Fact]
        public async Task Share_ForTheSameMemberInTwoExpenses_IsAccepted()
        {
            (Guid firstExpense, Guid memberId) = await NewExpenseAsync();
            Guid groupId = Guid.Parse((await ExpenseRows.ExpenseValueAsync(_database, firstExpense, "group_id"))!);
            Guid userId = Guid.Parse((await ExpenseRows.ExpenseValueAsync(_database, firstExpense, "created_by_user_id"))!);
            Guid secondExpense = await ExpenseRows.InsertExpenseAsync(_database, groupId, memberId, userId);
            await ExpenseRows.InsertShareAsync(_database, firstExpense, memberId);

            await ExpenseRows.InsertShareAsync(_database, secondExpense, memberId);

            Assert.Equal(1, await ExpenseRows.CountSharesOfAsync(_database, secondExpense));
        }

        [Fact]
        public async Task Share_OfZero_IsAccepted()
        {
            (Guid expenseId, Guid memberId) = await NewExpenseAsync();

            await ExpenseRows.InsertShareAsync(_database, expenseId, memberId, 0, 0);

            Assert.Equal(1, await ExpenseRows.CountSharesOfAsync(_database, expenseId));
        }

        [Fact]
        public async Task ShareMinor_Negative_ViolatesTheCheckConstraint()
        {
            (Guid expenseId, Guid memberId) = await NewExpenseAsync();

            await RejectedInsert.AssertAsync(
                () => ExpenseRows.InsertShareAsync(_database, expenseId, memberId, 0, -1),
                PostgresErrorCodes.CheckViolation);
        }

        [Fact]
        public async Task InputValue_Negative_ViolatesTheCheckConstraint()
        {
            (Guid expenseId, Guid memberId) = await NewExpenseAsync();

            await RejectedInsert.AssertAsync(
                () => ExpenseRows.InsertShareAsync(_database, expenseId, memberId, -1, 0),
                PostgresErrorCodes.CheckViolation);
        }

        [Fact]
        public async Task Share_ForAnUnknownExpense_ViolatesTheForeignKey()
        {
            (_, Guid memberId) = await NewExpenseAsync();

            await RejectedInsert.AssertAsync(
                () => ExpenseRows.InsertShareAsync(_database, Guid.CreateVersion7(), memberId),
                PostgresErrorCodes.ForeignKeyViolation);
        }

        [Fact]
        public async Task Share_ForAnUnknownMember_ViolatesTheForeignKey()
        {
            (Guid expenseId, _) = await NewExpenseAsync();

            await RejectedInsert.AssertAsync(
                () => ExpenseRows.InsertShareAsync(_database, expenseId, Guid.CreateVersion7()),
                PostgresErrorCodes.ForeignKeyViolation);
        }

        private async Task<(Guid ExpenseId, Guid MemberId)> NewExpenseAsync()
        {
            Guid userId = await UserRows.AddAsync(_database);
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_database, userId);
            Guid memberId = await GroupRows.CurrentMemberIdAsync(_database, groupId, userId);
            Guid expenseId = await ExpenseRows.InsertExpenseAsync(_database, groupId, memberId, userId);

            return (expenseId, memberId);
        }
    }
}
