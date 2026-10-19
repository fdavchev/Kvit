using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class PersonalCategoriesMigrationTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        private const string ForeignKeyViolation = "23503";
        private const string CheckViolation = "23514";
        private const string RestrictViolation = "23001";

        [Fact]
        public async Task Migration_PersonalCategories_IsInTheMigrationHistoryExactlyOnceAfterGuestUpgradedUsageEvent()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            List<string> applied = [.. await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)];

            Assert.Single(applied, migration => migration.EndsWith("_PersonalCategories", StringComparison.Ordinal));
            int previous = applied.FindIndex(migration => migration.EndsWith("_GuestUpgradedUsageEvent", StringComparison.Ordinal));
            int personalCategories = applied.FindIndex(migration => migration.EndsWith("_PersonalCategories", StringComparison.Ordinal));
            Assert.True(previous >= 0, "The GuestUpgradedUsageEvent migration is not applied.");
            Assert.True(personalCategories > previous, $"PersonalCategories is at {personalCategories} but GuestUpgradedUsageEvent is at {previous}.");
        }

        [Fact]
        public async Task CheckConstraints_AreExactlyTheThreeNamedOnes()
        {
            List<string?> names = await _database.QueryAsync(
                "SELECT conname FROM pg_constraint WHERE conrelid = 'categories'::regclass AND contype = 'c' ORDER BY conname");

            Assert.Equal(["ck_categories_built_in_or_custom", "ck_categories_color", "ck_categories_emoji_not_empty"], names);
        }

        [Fact]
        public async Task ForeignKeys_AreOnlyTheOwnerToUsersOneAndItRestrictsDeletes()
        {
            List<string?> foreignKeys = await _database.QueryAsync(
                "SELECT conname || ':' || confrelid::regclass::text || ':' || confdeltype::text FROM pg_constraint WHERE conrelid = 'categories'::regclass AND contype = 'f'");

            Assert.Equal("fk_categories_users_owner_user_id:users:r", Assert.Single(foreignKeys));
        }

        [Fact]
        public async Task Index_OnOwnerUserId_Exists()
        {
            List<string?> definitions = await _database.QueryAsync(
                "SELECT indexdef FROM pg_indexes WHERE tablename = 'categories' AND indexname = 'ix_categories_owner_user_id'");

            Assert.Contains("(owner_user_id)", Assert.Single(definitions));
        }

        [Fact]
        public async Task Insert_ColorOutsideTheTenNames_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => InsertCustomAsync(userId, "black"), CheckViolation);
        }

        [Fact]
        public async Task Insert_EachOfTheTenColors_IsAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);

            foreach (string color in new[] { "orange", "green", "yellow", "blue", "pink", "purple", "slate", "red", "teal", "gray" })
            {
                await InsertCustomAsync(userId, color);
            }

            List<string?> counts = await _database.QueryAsync("SELECT count(*)::text FROM categories WHERE owner_user_id = @owner", new NpgsqlParameter("owner", userId));
            Assert.Equal("10", Assert.Single(counts));
        }

        [Fact]
        public async Task Insert_OwnerThatIsNotAUser_IsRejected()
        {
            await RejectedInsert.AssertAsync(() => InsertCustomAsync(Guid.CreateVersion7(), "teal"), ForeignKeyViolation);
        }

        [Fact]
        public async Task Delete_AUserWhoOwnsACategory_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);
            await InsertCustomAsync(userId, "teal");

            await RejectedInsert.AssertAsync(
                () => _database.ExecuteAsync("DELETE FROM users WHERE id = @id", new NpgsqlParameter("id", userId)),
                RestrictViolation);
        }

        private Task InsertCustomAsync(Guid ownerUserId, string color)
        {
            return _database.ExecuteAsync(
                "INSERT INTO categories (id, owner_user_id, key, name, emoji, color, sort_order, archived_at) "
                + "VALUES (@id, @owner, NULL, @name, @emoji, @color, 11, NULL)",
                new NpgsqlParameter("id", Guid.CreateVersion7()),
                new NpgsqlParameter("owner", ownerUserId),
                new NpgsqlParameter("name", $"Name {Guid.NewGuid():N}"),
                new NpgsqlParameter("emoji", "\U0001F43E"),
                new NpgsqlParameter("color", color));
        }
    }
}
