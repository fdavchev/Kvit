using Kvit.Api.Tests.BudgetShares;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.PossibleDuplicates;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class DuplicateAnswersTableTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string ForeignKeyViolation = "23503";
        private const string UniqueViolation = "23505";
        private const string CheckViolation = "23514";
        private const string RestrictViolation = "23001";

        [Fact]
        public async Task Columns_AreTheExpectedOnesWithTheirTypesAndNullability()
        {
            List<string?> columns = await _app.QueryAsync(
                "SELECT column_name || ':' || data_type || ':' || is_nullable FROM information_schema.columns "
                + "WHERE table_schema = 'public' AND table_name = 'duplicate_answers' ORDER BY column_name");

            Assert.Equal(
                [
                    "answer:text:NO",
                    "created_at:timestamp with time zone:NO",
                    "expense_id:uuid:NO",
                    "id:uuid:NO",
                    "personal_entry_id:uuid:NO",
                    "user_id:uuid:NO",
                ],
                columns);
        }

        [Fact]
        public async Task Id_IsThePrimaryKey()
        {
            List<string?> primaryKeyColumns = await _app.QueryAsync(
                "SELECT a.attname FROM pg_index i JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY (i.indkey) "
                + "WHERE i.indrelid = 'duplicate_answers'::regclass AND i.indisprimary");

            Assert.Equal("id", Assert.Single(primaryKeyColumns));
        }

        [Fact]
        public async Task ForeignKeys_AreToUsersPersonalEntriesAndExpensesAndAllRestrictDeletes()
        {
            List<string?> foreignKeys = await _app.QueryAsync(
                "SELECT confrelid::regclass::text || ':' || confdeltype::text FROM pg_constraint "
                + "WHERE conrelid = 'duplicate_answers'::regclass AND contype = 'f' ORDER BY 1");

            Assert.Equal(["expenses:r", "personal_entries:r", "users:r"], foreignKeys);
        }

        [Fact]
        public async Task UniqueIndex_IsOnUserPersonalEntryAndExpense()
        {
            List<string?> definitions = await _app.QueryAsync(
                "SELECT indexdef FROM pg_indexes WHERE tablename = 'duplicate_answers' AND indexdef LIKE 'CREATE UNIQUE INDEX%' AND indexdef LIKE '%(user_id, personal_entry_id, expense_id)%'");

            Assert.Single(definitions);
        }

        [Fact]
        public async Task Insert_SameUserEntryAndExpenseTwice_IsRejected()
        {
            (Guid userId, Guid entryId, Guid expenseId) = await StartAsync();
            await DuplicateAnswerRows.InsertAsync(_app, userId, entryId, expenseId);

            await RejectedInsert.AssertAsync(() => DuplicateAnswerRows.InsertAsync(_app, userId, entryId, expenseId, "New"), UniqueViolation);
        }

        [Fact]
        public async Task Insert_SamePairForTwoUsers_IsAccepted()
        {
            (Guid userId, Guid entryId, Guid expenseId) = await StartAsync();
            Guid otherUserId = (await _app.SignUpAsync("Cvetan")).UserId;
            await DuplicateAnswerRows.InsertAsync(_app, userId, entryId, expenseId);

            await DuplicateAnswerRows.InsertAsync(_app, otherUserId, entryId, expenseId);

            Assert.Equal(1, await DuplicateAnswerRows.CountOfUserAsync(_app, otherUserId));
        }

        [Theory]
        [InlineData("Same")]
        [InlineData("New")]
        public async Task Insert_BothAnswers_AreAccepted(string answer)
        {
            (Guid userId, Guid entryId, Guid expenseId) = await StartAsync();

            await DuplicateAnswerRows.InsertAsync(_app, userId, entryId, expenseId, answer);

            Assert.Equal(answer, await DuplicateAnswerRows.AnswerOfAsync(_app, userId, entryId, expenseId));
        }

        [Theory]
        [InlineData("")]
        [InlineData("Maybe")]
        public async Task Insert_AnswerThatIsNotSameOrNew_IsRejected(string answer)
        {
            (Guid userId, Guid entryId, Guid expenseId) = await StartAsync();

            await RejectedInsert.AssertAsync(() => DuplicateAnswerRows.InsertAsync(_app, userId, entryId, expenseId, answer), CheckViolation);
        }

        [Fact]
        public async Task Insert_UserThatDoesNotExist_IsRejected()
        {
            (_, Guid entryId, Guid expenseId) = await StartAsync();

            await RejectedInsert.AssertAsync(() => DuplicateAnswerRows.InsertAsync(_app, Guid.CreateVersion7(), entryId, expenseId), ForeignKeyViolation);
        }

        [Fact]
        public async Task Insert_EntryThatDoesNotExist_IsRejected()
        {
            (Guid userId, _, Guid expenseId) = await StartAsync();

            await RejectedInsert.AssertAsync(() => DuplicateAnswerRows.InsertAsync(_app, userId, Guid.CreateVersion7(), expenseId), ForeignKeyViolation);
        }

        [Fact]
        public async Task Insert_ExpenseThatDoesNotExist_IsRejected()
        {
            (Guid userId, Guid entryId, _) = await StartAsync();

            await RejectedInsert.AssertAsync(() => DuplicateAnswerRows.InsertAsync(_app, userId, entryId, Guid.CreateVersion7()), ForeignKeyViolation);
        }

        [Fact]
        public async Task Delete_AnEntryThatHasAnAnswer_IsRejected()
        {
            (Guid userId, Guid entryId, Guid expenseId) = await StartAsync();
            await DuplicateAnswerRows.InsertAsync(_app, userId, entryId, expenseId);

            await RejectedInsert.AssertAsync(
                () => _app.ExecuteAsync("DELETE FROM personal_entries WHERE id = @id", new NpgsqlParameter("id", entryId)),
                RestrictViolation);
        }

        private async Task<(Guid UserId, Guid EntryId, Guid ExpenseId)> StartAsync()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);

            return (group.Member.UserId, entryId, expenseId);
        }
    }
}
