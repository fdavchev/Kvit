using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class SchemaTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        private const string TableNamesSql =
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE' AND table_name <> '__EFMigrationsHistory'";

        private const string ColumnNamesSql =
            "SELECT table_name || '.' || column_name FROM information_schema.columns WHERE table_schema = 'public' AND table_name <> '__EFMigrationsHistory'";

        private const string IndexNamesSql =
            "SELECT tablename || '.' || indexname FROM pg_indexes WHERE schemaname = 'public' AND tablename <> '__EFMigrationsHistory'";

        private const string ConstraintNamesSql =
            "SELECT r.relname || '.' || c.conname FROM pg_constraint c JOIN pg_class r ON r.oid = c.conrelid JOIN pg_namespace n ON n.oid = r.relnamespace WHERE n.nspname = 'public' AND r.relname <> '__EFMigrationsHistory'";

        [Fact]
        public async Task Migration_CreatesExactlyTheExpectedTables()
        {
            string[] expectedTables =
            [
                "activity_events",
                "categories",
                "data_protection_keys",
                "exchange_rates",
                "group_members",
                "groups",
                "role_claims",
                "roles",
                "usage_events",
                "user_claims",
                "user_logins",
                "user_roles",
                "user_tokens",
                "users",
            ];

            List<string?> tables = await _database.QueryAsync(TableNamesSql);

            Assert.Equal(expectedTables, tables.OfType<string>().Order(StringComparer.Ordinal));
        }

        [Fact]
        public async Task Migration_CreatesTheEfMigrationsHistoryTable()
        {
            List<string?> tables = await _database.QueryAsync(
                "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '__EFMigrationsHistory'");

            Assert.Equal("__EFMigrationsHistory", Assert.Single(tables));
        }

        [Fact]
        public async Task TableNames_AreAllLowercase()
        {
            List<string?> tables = await _database.QueryAsync(TableNamesSql);

            AssertAllLowercase(tables);
        }

        [Fact]
        public async Task ColumnNames_AreAllLowercase()
        {
            List<string?> columns = await _database.QueryAsync(ColumnNamesSql);

            AssertAllLowercase(columns);
        }

        [Fact]
        public async Task IndexNames_AreAllLowercase()
        {
            List<string?> indexes = await _database.QueryAsync(IndexNamesSql);

            AssertAllLowercase(indexes);
        }

        [Fact]
        public async Task ConstraintNames_AreAllLowercase()
        {
            List<string?> constraints = await _database.QueryAsync(ConstraintNamesSql);

            AssertAllLowercase(constraints);
        }

        private static void AssertAllLowercase(List<string?> names)
        {
            List<string> present = [.. names.OfType<string>()];
            List<string> withUppercase = [.. present.Where(name => name != name.ToLowerInvariant())];

            Assert.NotEmpty(present);
            Assert.Empty(withUppercase);
        }
    }
}
