using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class ExpensesTableTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        [Theory]
        [InlineData("id", "uuid")]
        [InlineData("group_id", "uuid")]
        [InlineData("amount_minor", "bigint")]
        [InlineData("currency", "text")]
        [InlineData("expense_date", "date")]
        [InlineData("category_id", "uuid")]
        [InlineData("paid_by_member_id", "uuid")]
        [InlineData("split_type", "text")]
        [InlineData("mkd_per_eur", "numeric")]
        [InlineData("rate_date", "date")]
        [InlineData("created_by_user_id", "uuid")]
        [InlineData("created_at", "timestamp with time zone")]
        [InlineData("updated_by_user_id", "uuid")]
        [InlineData("updated_at", "timestamp with time zone")]
        [InlineData("deleted_at", "timestamp with time zone")]
        [InlineData("deleted_by_user_id", "uuid")]
        [InlineData("client_request_id", "uuid")]
        public async Task Column_HasTheExpectedType(string column, string expectedDataType)
        {
            string? dataType = await SchemaQueries.DataTypeAsync(_database, "expenses", column);

            Assert.Equal(expectedDataType, dataType);
        }

        [Theory]
        [InlineData("id", "NO")]
        [InlineData("group_id", "NO")]
        [InlineData("title", "YES")]
        [InlineData("note", "YES")]
        [InlineData("amount_minor", "NO")]
        [InlineData("currency", "NO")]
        [InlineData("expense_date", "NO")]
        [InlineData("category_id", "YES")]
        [InlineData("paid_by_member_id", "NO")]
        [InlineData("split_type", "NO")]
        [InlineData("mkd_per_eur", "NO")]
        [InlineData("rate_date", "NO")]
        [InlineData("created_by_user_id", "NO")]
        [InlineData("created_at", "NO")]
        [InlineData("updated_by_user_id", "YES")]
        [InlineData("updated_at", "YES")]
        [InlineData("deleted_at", "YES")]
        [InlineData("deleted_by_user_id", "YES")]
        public async Task Column_HasTheExpectedNullability(string column, string expectedIsNullable)
        {
            string? isNullable = await SchemaQueries.IsNullableAsync(_database, "expenses", column);

            Assert.Equal(expectedIsNullable, isNullable);
        }

        [Fact]
        public async Task Table_HasExactlyTheNineteenColumns()
        {
            List<string?> columns = await SchemaQueries.ColumnNamesAsync(_database, "expenses");

            string[] expected = ExpectedColumns();
            Assert.Equal(expected, columns);
        }

        [Fact]
        public async Task MkdPerEur_IsNumeric10WithFourDecimals()
        {
            List<string?> precisionAndScale = await _database.QueryAsync(
                "SELECT numeric_precision || ',' || numeric_scale FROM information_schema.columns "
                + "WHERE table_schema = 'public' AND table_name = 'expenses' AND column_name = 'mkd_per_eur'");

            Assert.Equal("10,4", Assert.Single(precisionAndScale));
        }

        [Fact]
        public async Task Id_IsThePrimaryKey()
        {
            List<string?> primaryKeyColumns = await _database.QueryAsync(
                "SELECT a.attname FROM pg_index i JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY (i.indkey) "
                + "WHERE i.indrelid = 'expenses'::regclass AND i.indisprimary");

            Assert.Equal("id", Assert.Single(primaryKeyColumns));
        }

        [Fact]
        public async Task ValidExpense_IsAccepted()
        {
            (Guid groupId, Guid memberId, Guid userId) = await NewGroupAsync();

            await ExpenseRows.InsertExpenseAsync(_database, groupId, memberId, userId);

            Assert.Equal(1, await ExpenseRows.CountExpensesAsync(_database, groupId));
        }

        [Fact]
        public async Task ClientRequestId_UsedByTwoExpenses_ViolatesTheUniqueIndex()
        {
            (Guid groupId, Guid memberId, Guid userId) = await NewGroupAsync();
            Guid clientRequestId = Guid.CreateVersion7();
            await ExpenseRows.InsertExpenseAsync(_database, groupId, memberId, userId, clientRequestId: clientRequestId);

            await RejectedInsert.AssertAsync(
                () => ExpenseRows.InsertExpenseAsync(_database, groupId, memberId, userId, clientRequestId: clientRequestId),
                PostgresErrorCodes.UniqueViolation);
        }

        [Fact]
        public async Task ClientRequestId_UsedInTwoGroups_ViolatesTheUniqueIndex()
        {
            (Guid firstGroup, Guid firstMember, Guid userId) = await NewGroupAsync();
            (Guid secondGroup, Guid secondMember, _) = await NewGroupAsync();
            Guid clientRequestId = Guid.CreateVersion7();
            await ExpenseRows.InsertExpenseAsync(_database, firstGroup, firstMember, userId, clientRequestId: clientRequestId);

            await RejectedInsert.AssertAsync(
                () => ExpenseRows.InsertExpenseAsync(_database, secondGroup, secondMember, userId, clientRequestId: clientRequestId),
                PostgresErrorCodes.UniqueViolation);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task AmountMinor_ZeroOrLess_ViolatesTheCheckConstraint(long amountMinor)
        {
            (Guid groupId, Guid memberId, Guid userId) = await NewGroupAsync();

            await RejectedInsert.AssertAsync(
                () => ExpenseRows.InsertExpenseAsync(_database, groupId, memberId, userId, amountMinor: amountMinor),
                PostgresErrorCodes.CheckViolation);
        }

        [Fact]
        public async Task AmountMinor_OfOne_IsAccepted()
        {
            (Guid groupId, Guid memberId, Guid userId) = await NewGroupAsync();

            await ExpenseRows.InsertExpenseAsync(_database, groupId, memberId, userId, amountMinor: 1);

            Assert.Equal(1, await ExpenseRows.CountExpensesAsync(_database, groupId));
        }

        [Theory]
        [InlineData("MKD")]
        [InlineData("EUR")]
        public async Task Currency_OfTheTwoKnownNames_IsAccepted(string currency)
        {
            (Guid groupId, Guid memberId, Guid userId) = await NewGroupAsync();

            await ExpenseRows.InsertExpenseAsync(_database, groupId, memberId, userId, currency: currency);

            Assert.Equal(1, await ExpenseRows.CountExpensesAsync(_database, groupId));
        }

        [Theory]
        [InlineData("USD")]
        [InlineData("mkd")]
        [InlineData("eur")]
        [InlineData("")]
        public async Task Currency_OutsideTheTwoKnownNames_ViolatesTheCheckConstraint(string currency)
        {
            (Guid groupId, Guid memberId, Guid userId) = await NewGroupAsync();

            await RejectedInsert.AssertAsync(
                () => ExpenseRows.InsertExpenseAsync(_database, groupId, memberId, userId, currency: currency),
                PostgresErrorCodes.CheckViolation);
        }

        [Theory]
        [InlineData("Equal")]
        [InlineData("Exact")]
        [InlineData("Percentage")]
        [InlineData("Shares")]
        public async Task SplitType_OfTheFourKnownNames_IsAccepted(string splitType)
        {
            (Guid groupId, Guid memberId, Guid userId) = await NewGroupAsync();

            await ExpenseRows.InsertExpenseAsync(_database, groupId, memberId, userId, splitType: splitType);

            Assert.Equal(1, await ExpenseRows.CountExpensesAsync(_database, groupId));
        }

        [Theory]
        [InlineData("equal")]
        [InlineData("EqualPlusExtras")]
        [InlineData("Unknown")]
        [InlineData("")]
        public async Task SplitType_OutsideTheFourKnownNames_ViolatesTheCheckConstraint(string splitType)
        {
            (Guid groupId, Guid memberId, Guid userId) = await NewGroupAsync();

            await RejectedInsert.AssertAsync(
                () => ExpenseRows.InsertExpenseAsync(_database, groupId, memberId, userId, splitType: splitType),
                PostgresErrorCodes.CheckViolation);
        }

        [Fact]
        public async Task Title_Of80Characters_IsAccepted()
        {
            (Guid groupId, Guid memberId, Guid userId) = await NewGroupAsync();

            await ExpenseRows.InsertExpenseAsync(_database, groupId, memberId, userId, title: new string('x', 80));

            Assert.Equal(1, await ExpenseRows.CountExpensesAsync(_database, groupId));
        }

        [Fact]
        public async Task Title_Of81Characters_ViolatesTheCheckConstraint()
        {
            (Guid groupId, Guid memberId, Guid userId) = await NewGroupAsync();

            await RejectedInsert.AssertAsync(
                () => ExpenseRows.InsertExpenseAsync(_database, groupId, memberId, userId, title: new string('x', 81)),
                PostgresErrorCodes.CheckViolation);
        }

        [Fact]
        public async Task Note_Of500Characters_IsAccepted()
        {
            (Guid groupId, Guid memberId, Guid userId) = await NewGroupAsync();

            await ExpenseRows.InsertExpenseAsync(_database, groupId, memberId, userId, note: new string('x', 500));

            Assert.Equal(1, await ExpenseRows.CountExpensesAsync(_database, groupId));
        }

        [Fact]
        public async Task Note_Of501Characters_ViolatesTheCheckConstraint()
        {
            (Guid groupId, Guid memberId, Guid userId) = await NewGroupAsync();

            await RejectedInsert.AssertAsync(
                () => ExpenseRows.InsertExpenseAsync(_database, groupId, memberId, userId, note: new string('x', 501)),
                PostgresErrorCodes.CheckViolation);
        }

        [Fact]
        public async Task Expense_ForAnUnknownGroup_ViolatesTheForeignKey()
        {
            (_, Guid memberId, Guid userId) = await NewGroupAsync();

            await RejectedInsert.AssertAsync(
                () => ExpenseRows.InsertExpenseAsync(_database, Guid.CreateVersion7(), memberId, userId),
                PostgresErrorCodes.ForeignKeyViolation);
        }

        [Fact]
        public async Task Expense_WithAnUnknownPayer_ViolatesTheForeignKey()
        {
            (Guid groupId, _, Guid userId) = await NewGroupAsync();

            await RejectedInsert.AssertAsync(
                () => ExpenseRows.InsertExpenseAsync(_database, groupId, Guid.CreateVersion7(), userId),
                PostgresErrorCodes.ForeignKeyViolation);
        }

        [Fact]
        public async Task Expense_WithAnUnknownCategory_ViolatesTheForeignKey()
        {
            (Guid groupId, Guid memberId, Guid userId) = await NewGroupAsync();

            await RejectedInsert.AssertAsync(
                () => ExpenseRows.InsertExpenseAsync(_database, groupId, memberId, userId, categoryId: Guid.CreateVersion7()),
                PostgresErrorCodes.ForeignKeyViolation);
        }

        [Fact]
        public async Task Expense_WithABuiltInCategory_IsAccepted()
        {
            (Guid groupId, Guid memberId, Guid userId) = await NewGroupAsync();
            List<string?> categoryIds = await _database.QueryAsync("SELECT id::text FROM categories WHERE key = 'food'");

            await ExpenseRows.InsertExpenseAsync(_database, groupId, memberId, userId, categoryId: Guid.Parse(Assert.Single(categoryIds)!));

            Assert.Equal(1, await ExpenseRows.CountExpensesAsync(_database, groupId));
        }

        [Fact]
        public async Task Expense_WithoutACategory_IsAccepted()
        {
            (Guid groupId, Guid memberId, Guid userId) = await NewGroupAsync();

            await ExpenseRows.InsertExpenseAsync(_database, groupId, memberId, userId, categoryId: null);

            Assert.Null(await ExpenseRows.ExpenseValueAsync(_database, await OnlyExpenseIdAsync(groupId), "category_id"));
        }

        [Fact]
        public async Task Expense_ByAnUnknownUser_IsAcceptedBecauseTheUserColumnsHaveNoForeignKey()
        {
            (Guid groupId, Guid memberId, _) = await NewGroupAsync();

            await ExpenseRows.InsertExpenseAsync(_database, groupId, memberId, Guid.CreateVersion7());

            Assert.Equal(1, await ExpenseRows.CountExpensesAsync(_database, groupId));
        }

        [Fact]
        public async Task SoftDeletedExpense_IsAccepted()
        {
            (Guid groupId, Guid memberId, Guid userId) = await NewGroupAsync();

            await ExpenseRows.InsertExpenseAsync(_database, groupId, memberId, userId, deletedAt: DateTimeOffset.UtcNow);

            Assert.Equal(userId.ToString(), await ExpenseRows.ExpenseValueAsync(_database, await OnlyExpenseIdAsync(groupId), "deleted_by_user_id"));
        }

        private static string[] ExpectedColumns()
        {
            return
            [
                "amount_minor",
                "category_id",
                "client_request_id",
                "created_at",
                "created_by_user_id",
                "currency",
                "deleted_at",
                "deleted_by_user_id",
                "expense_date",
                "group_id",
                "id",
                "mkd_per_eur",
                "note",
                "paid_by_member_id",
                "rate_date",
                "split_type",
                "title",
                "updated_at",
                "updated_by_user_id",
            ];
        }

        private async Task<(Guid GroupId, Guid MemberId, Guid UserId)> NewGroupAsync()
        {
            Guid userId = await UserRows.AddAsync(_database);
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_database, userId);
            Guid memberId = await GroupRows.CurrentMemberIdAsync(_database, groupId, userId);

            return (groupId, memberId, userId);
        }

        private async Task<Guid> OnlyExpenseIdAsync(Guid groupId)
        {
            List<string?> ids = await _database.QueryAsync(
                "SELECT id::text FROM expenses WHERE group_id = @group_id",
                new NpgsqlParameter("group_id", groupId));

            return Guid.Parse(Assert.Single(ids)!);
        }
    }
}
