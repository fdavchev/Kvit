using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class PersonalCategoryNameUniqueMigrationTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        private const string UniqueViolation = "23505";
        private const string IndexName = "ix_categories_owner_user_id_active_lower_name";

        [Fact]
        public async Task Migration_PersonalCategoryNameUnique_IsInTheMigrationHistoryExactlyOnceAfterPersonalEntries()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            List<string> applied = [.. await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)];

            Assert.Single(applied, migration => migration.EndsWith("_PersonalCategoryNameUnique", StringComparison.Ordinal));
            int previous = applied.FindIndex(migration => migration.EndsWith("_PersonalEntries", StringComparison.Ordinal));
            int nameUnique = applied.FindIndex(migration => migration.EndsWith("_PersonalCategoryNameUnique", StringComparison.Ordinal));
            Assert.True(previous >= 0, "The PersonalEntries migration is not applied.");
            Assert.True(nameUnique > previous, $"PersonalCategoryNameUnique is at {nameUnique} but PersonalEntries is at {previous}.");
        }

        [Fact]
        public async Task Index_IsUniqueOnTheOwnerAndTheLowerCaseNameOfActiveCustomCategoriesOnly()
        {
            List<string?> definitions = await _database.QueryAsync(
                "SELECT indexdef FROM pg_indexes WHERE tablename = 'categories' AND indexname = @name",
                new NpgsqlParameter("name", IndexName));

            string definition = Assert.Single(definitions)!;
            Assert.Contains("CREATE UNIQUE INDEX", definition, StringComparison.Ordinal);
            Assert.Contains("(owner_user_id, lower(name))", definition, StringComparison.Ordinal);
            Assert.Contains("owner_user_id IS NOT NULL", definition, StringComparison.Ordinal);
            Assert.Contains("archived_at IS NULL", definition, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("Pets", "pets")]
        [InlineData("Pets", "PETS")]
        [InlineData("Pets", "Pets")]
        [InlineData("Храна", "храна")]
        [InlineData("Миленичиња", "МИЛЕНИЧИЊА")]
        public async Task Insert_SameNameIgnoringLetterCaseForTheSameOwner_IsRejected(string firstName, string secondName)
        {
            Guid userId = await UserRows.AddAsync(_database);
            await InsertCustomAsync(userId, firstName);

            await RejectedInsert.AssertAsync(() => InsertCustomAsync(userId, secondName), UniqueViolation);
        }

        [Fact]
        public async Task Insert_SameNameForAnotherOwner_IsAccepted()
        {
            Guid firstUserId = await UserRows.AddAsync(_database);
            Guid secondUserId = await UserRows.AddAsync(_database);
            await InsertCustomAsync(firstUserId, "Pets");

            await InsertCustomAsync(secondUserId, "pets");

            Assert.Equal(1, await CountActiveNamedAsync(secondUserId, "pets"));
        }

        [Fact]
        public async Task Insert_SameNameAsAnArchivedCategoryOfTheSameOwner_IsAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);
            await InsertCustomAsync(userId, "Pets", DateTimeOffset.UtcNow);

            await InsertCustomAsync(userId, "pets");

            Assert.Equal(1, await CountActiveNamedAsync(userId, "pets"));
        }

        [Fact]
        public async Task Update_BringingBackAnArchivedCategoryWhoseNameAnActiveOneHas_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);
            Guid archived = await InsertCustomAsync(userId, "Pets", DateTimeOffset.UtcNow);
            await InsertCustomAsync(userId, "pets");

            await RejectedInsert.AssertAsync(
                () => _database.ExecuteAsync("UPDATE categories SET archived_at = NULL WHERE id = @id", new NpgsqlParameter("id", archived)),
                UniqueViolation);
        }

        private async Task<Guid> InsertCustomAsync(Guid ownerUserId, string name, DateTimeOffset? archivedAt = null)
        {
            Guid categoryId = Guid.CreateVersion7();
            await _database.ExecuteAsync(
                "INSERT INTO categories (id, owner_user_id, key, name, emoji, color, sort_order, archived_at) "
                + "VALUES (@id, @owner, NULL, @name, @emoji, 'teal', 11, @archived_at)",
                new NpgsqlParameter("id", categoryId),
                new NpgsqlParameter("owner", ownerUserId),
                new NpgsqlParameter("name", name),
                new NpgsqlParameter("emoji", "\U0001F43E"),
                new NpgsqlParameter("archived_at", (object?)archivedAt ?? DBNull.Value));

            return categoryId;
        }

        private async Task<int> CountActiveNamedAsync(Guid ownerUserId, string name)
        {
            List<string?> counts = await _database.QueryAsync(
                "SELECT count(*)::text FROM categories WHERE owner_user_id = @owner AND name = @name AND archived_at IS NULL",
                new NpgsqlParameter("owner", ownerUserId),
                new NpgsqlParameter("name", name));

            return int.Parse(Assert.Single(counts)!);
        }
    }
}
