using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public static class DuplicateAnswerRows
    {
        public static async Task<Guid> InsertAsync(MigratedDatabase database, Guid userId, Guid entryId, Guid expenseId, string answer = "Same")
        {
            Guid id = Guid.CreateVersion7();
            await database.ExecuteAsync(
                "INSERT INTO duplicate_answers (id, user_id, personal_entry_id, expense_id, answer, created_at) "
                + "VALUES (@id, @user_id, @entry_id, @expense_id, @answer, now())",
                new NpgsqlParameter("id", id),
                new NpgsqlParameter("user_id", userId),
                new NpgsqlParameter("entry_id", entryId),
                new NpgsqlParameter("expense_id", expenseId),
                new NpgsqlParameter("answer", answer));

            return id;
        }

        public static async Task<string?> AnswerOfAsync(MigratedDatabase database, Guid userId, Guid entryId, Guid expenseId)
        {
            List<string?> answers = await database.QueryAsync(
                "SELECT answer FROM duplicate_answers WHERE user_id = @user_id AND personal_entry_id = @entry_id AND expense_id = @expense_id",
                new NpgsqlParameter("user_id", userId),
                new NpgsqlParameter("entry_id", entryId),
                new NpgsqlParameter("expense_id", expenseId));

            return answers.SingleOrDefault();
        }

        public static async Task<string?> CreatedAtOfAsync(MigratedDatabase database, Guid userId, Guid entryId, Guid expenseId)
        {
            List<string?> values = await database.QueryAsync(
                "SELECT created_at::text FROM duplicate_answers WHERE user_id = @user_id AND personal_entry_id = @entry_id AND expense_id = @expense_id",
                new NpgsqlParameter("user_id", userId),
                new NpgsqlParameter("entry_id", entryId),
                new NpgsqlParameter("expense_id", expenseId));

            return Assert.Single(values);
        }

        public static async Task<int> CountOfUserAsync(MigratedDatabase database, Guid userId)
        {
            List<string?> counts = await database.QueryAsync(
                "SELECT count(*)::text FROM duplicate_answers WHERE user_id = @user_id",
                new NpgsqlParameter("user_id", userId));

            return int.Parse(Assert.Single(counts)!);
        }

        public static Task DeleteOfUserAsync(MigratedDatabase database, Guid userId)
        {
            return database.ExecuteAsync(
                "DELETE FROM duplicate_answers WHERE user_id = @user_id",
                new NpgsqlParameter("user_id", userId));
        }

        public static async Task<string> SnapshotAsync(MigratedDatabase database)
        {
            List<string?> rows = await database.QueryAsync(
                "SELECT concat_ws('|', id, user_id, personal_entry_id, expense_id, answer, created_at) FROM duplicate_answers ORDER BY id");

            return string.Join("\n", rows);
        }
    }
}
