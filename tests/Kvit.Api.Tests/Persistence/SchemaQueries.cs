using Npgsql;

namespace Kvit.Api.Tests.Persistence
{
    public static class SchemaQueries
    {
        public static Task<string?> DataTypeAsync(MigratedDatabase database, string table, string column)
        {
            return SingleValueAsync(database, "data_type", table, column);
        }

        public static Task<string?> IsNullableAsync(MigratedDatabase database, string table, string column)
        {
            return SingleValueAsync(database, "is_nullable", table, column);
        }

        public static Task<string?> MaxLengthAsync(MigratedDatabase database, string table, string column)
        {
            return SingleValueAsync(database, "character_maximum_length::text", table, column);
        }

        public static Task<string?> DefaultAsync(MigratedDatabase database, string table, string column)
        {
            return SingleValueAsync(database, "column_default", table, column);
        }

        public static Task<string?> IsIdentityAsync(MigratedDatabase database, string table, string column)
        {
            return SingleValueAsync(database, "is_identity", table, column);
        }

        public static Task<List<string?>> ColumnNamesAsync(MigratedDatabase database, string table)
        {
            return database.QueryAsync(
                "SELECT column_name FROM information_schema.columns WHERE table_schema = 'public' AND table_name = @table ORDER BY column_name",
                new NpgsqlParameter("table", table));
        }

        private static async Task<string?> SingleValueAsync(MigratedDatabase database, string informationSchemaColumn, string table, string column)
        {
            List<string?> values = await database.QueryAsync(
                $"SELECT {informationSchemaColumn} FROM information_schema.columns WHERE table_schema = 'public' AND table_name = @table AND column_name = @column",
                new NpgsqlParameter("table", table),
                new NpgsqlParameter("column", column));

            return values.SingleOrDefault();
        }
    }
}
