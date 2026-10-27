using Kvit.Domain.Entities;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class BudgetLimitsTableTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        private const string ForeignKeyViolation = "23503";
        private const string CheckViolation = "23514";
        private const string UniqueViolation = "23505";
        private const string NotNullViolation = "23502";
        private const string RestrictViolation = "23001";

        [Fact]
        public async Task Columns_AreTheExpectedOnesWithTheirTypesAndNullability()
        {
            List<string?> columns = await _database.QueryAsync(
                "SELECT column_name || ':' || data_type || ':' || is_nullable FROM information_schema.columns "
                + "WHERE table_schema = 'public' AND table_name = 'budget_limits' ORDER BY column_name");

            Assert.Equal(
                [
                    "category_id:uuid:YES",
                    "created_at:timestamp with time zone:NO",
                    "effective_from:date:NO",
                    "id:uuid:NO",
                    "limit_minor:bigint:YES",
                    "updated_at:timestamp with time zone:YES",
                    "user_id:uuid:NO",
                ],
                columns);
        }

        [Fact]
        public async Task Id_IsThePrimaryKey()
        {
            List<string?> primaryKeyColumns = await _database.QueryAsync(
                "SELECT a.attname FROM pg_index i JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY (i.indkey) "
                + "WHERE i.indrelid = 'budget_limits'::regclass AND i.indisprimary");

            Assert.Equal("id", Assert.Single(primaryKeyColumns));
        }

        [Fact]
        public async Task CheckConstraints_AreExactlyTheTwoNamedOnes()
        {
            List<string?> names = await _database.QueryAsync(
                "SELECT conname FROM pg_constraint WHERE conrelid = 'budget_limits'::regclass AND contype = 'c' ORDER BY conname");

            Assert.Equal(["ck_budget_limits_effective_from_first_day", "ck_budget_limits_limit_minor_valid"], names);
        }

        [Fact]
        public async Task ForeignKeys_AreToUsersAndCategoriesAndBothRestrictDeletes()
        {
            List<string?> foreignKeys = await _database.QueryAsync(
                "SELECT conname || ':' || confrelid::regclass::text || ':' || confdeltype::text FROM pg_constraint "
                + "WHERE conrelid = 'budget_limits'::regclass AND contype = 'f' ORDER BY conname");

            Assert.Equal(
                [
                    "fk_budget_limits_categories_category_id:categories:r",
                    "fk_budget_limits_users_user_id:users:r",
                ],
                foreignKeys);
        }

        [Fact]
        public async Task UniqueIndex_IsOnUserCategoryAndEffectiveFromAndTreatsANullCategoryAsOneValue()
        {
            List<string?> definitions = await _database.QueryAsync(
                "SELECT indexdef FROM pg_indexes WHERE tablename = 'budget_limits' AND indexname = 'ix_budget_limits_user_id_category_id_effective_from'");

            string definition = Assert.Single(definitions)!;
            Assert.Contains("CREATE UNIQUE INDEX", definition);
            Assert.Contains("(user_id, category_id, effective_from)", definition);
            Assert.Contains("NULLS NOT DISTINCT", definition);
        }

        [Fact]
        public async Task Insert_MonthRowWithoutACategory_IsAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await BudgetLimitRows.InsertAsync(_database, userId);

            Assert.Equal(1, await BudgetLimitRows.CountOfUserAsync(_database, userId));
        }

        [Fact]
        public async Task Insert_CategoryRowForABuiltInCategory_IsAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await BudgetLimitRows.InsertAsync(_database, userId, BudgetLimitRows.BuiltInFoodId);

            Assert.Equal(1, await BudgetLimitRows.CountOfUserAsync(_database, userId));
        }

        [Fact]
        public async Task Insert_NoLimitRow_IsAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await BudgetLimitRows.InsertAsync(_database, userId, limitMinor: null);

            Assert.Equal(1, await BudgetLimitRows.CountOfUserAsync(_database, userId));
        }

        [Fact]
        public async Task Insert_SameUserAndCategoryInDifferentMonths_IsAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);
            await BudgetLimitRows.InsertAsync(_database, userId, BudgetLimitRows.BuiltInFoodId, new DateOnly(2026, 9, 1));

            await BudgetLimitRows.InsertAsync(_database, userId, BudgetLimitRows.BuiltInFoodId, new DateOnly(2026, 10, 1));
            await BudgetLimitRows.InsertAsync(_database, userId, null, new DateOnly(2026, 9, 1));
            await BudgetLimitRows.InsertAsync(_database, userId, null, new DateOnly(2026, 10, 1));

            Assert.Equal(4, await BudgetLimitRows.CountOfUserAsync(_database, userId));
        }

        [Fact]
        public async Task Insert_SameCategoryAndMonthForTwoUsers_IsAccepted()
        {
            Guid first = await UserRows.AddAsync(_database);
            Guid second = await UserRows.AddAsync(_database);
            await BudgetLimitRows.InsertAsync(_database, first, BudgetLimitRows.BuiltInFoodId);
            await BudgetLimitRows.InsertAsync(_database, first, null);

            await BudgetLimitRows.InsertAsync(_database, second, BudgetLimitRows.BuiltInFoodId);
            await BudgetLimitRows.InsertAsync(_database, second, null);

            Assert.Equal(2, await BudgetLimitRows.CountOfUserAsync(_database, second));
        }

        [Fact]
        public async Task Insert_SecondWholeMonthRowForTheSameUserAndMonth_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);
            await BudgetLimitRows.InsertAsync(_database, userId, null);

            await RejectedInsert.AssertAsync(() => BudgetLimitRows.InsertAsync(_database, userId, null, limitMinor: 900_000), UniqueViolation);
        }

        [Fact]
        public async Task Insert_SecondCategoryRowForTheSameUserCategoryAndMonth_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);
            await BudgetLimitRows.InsertAsync(_database, userId, BudgetLimitRows.BuiltInFoodId);

            await RejectedInsert.AssertAsync(() => BudgetLimitRows.InsertAsync(_database, userId, BudgetLimitRows.BuiltInFoodId, limitMinor: 900_000), UniqueViolation);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        [InlineData(1)]
        [InlineData(150)]
        [InlineData(100_050)]
        public async Task Insert_LimitThatIsNotAPositiveWholeDenarAmount_IsRejected(long limitMinor)
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => BudgetLimitRows.InsertAsync(_database, userId, limitMinor: limitMinor), CheckViolation);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(800_000)]
        [InlineData(999_999_999_900)]
        public async Task Insert_PositiveWholeDenarLimit_IsAccepted(long limitMinor)
        {
            Guid userId = await UserRows.AddAsync(_database);

            await BudgetLimitRows.InsertAsync(_database, userId, limitMinor: limitMinor);

            Assert.Equal(1, await BudgetLimitRows.CountOfUserAsync(_database, userId));
        }

        [Theory]
        [InlineData(2026, 10, 2)]
        [InlineData(2026, 10, 15)]
        [InlineData(2026, 10, 31)]
        public async Task Insert_EffectiveFromThatIsNotTheFirstDayOfAMonth_IsRejected(int year, int month, int day)
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => BudgetLimitRows.InsertAsync(_database, userId, effectiveFrom: new DateOnly(year, month, day)), CheckViolation);
        }

        [Fact]
        public async Task Update_EffectiveFromToAnotherDayOfTheMonth_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);
            Guid id = await BudgetLimitRows.InsertAsync(_database, userId);

            await RejectedInsert.AssertAsync(
                () => _database.ExecuteAsync(
                    "UPDATE budget_limits SET effective_from = DATE '2026-10-05' WHERE id = @id",
                    new NpgsqlParameter("id", id)),
                CheckViolation);
        }

        [Fact]
        public async Task Insert_UserThatDoesNotExist_IsRejected()
        {
            await RejectedInsert.AssertAsync(() => BudgetLimitRows.InsertAsync(_database, Guid.CreateVersion7()), ForeignKeyViolation);
        }

        [Fact]
        public async Task Insert_CategoryThatDoesNotExist_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => BudgetLimitRows.InsertAsync(_database, userId, Guid.CreateVersion7()), ForeignKeyViolation);
        }

        [Fact]
        public async Task Insert_WithoutAMonthItStartsApplying_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(
                () => _database.ExecuteAsync(
                    "INSERT INTO budget_limits (id, user_id, category_id, limit_minor, created_at) VALUES (@id, @user_id, NULL, 800000, now())",
                    new NpgsqlParameter("id", Guid.CreateVersion7()),
                    new NpgsqlParameter("user_id", userId)),
                NotNullViolation);
        }

        [Fact]
        public async Task Delete_AUserWithALimit_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);
            await BudgetLimitRows.InsertAsync(_database, userId);

            await RejectedInsert.AssertAsync(
                () => _database.ExecuteAsync("DELETE FROM users WHERE id = @id", new NpgsqlParameter("id", userId)),
                RestrictViolation);
        }

        [Fact]
        public async Task Delete_ACategoryWithALimit_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);
            Guid categoryId = Guid.CreateVersion7();
            await _database.ExecuteAsync(
                "INSERT INTO categories (id, owner_user_id, key, name, emoji, color, sort_order) VALUES (@id, @owner, NULL, 'Pets', 'x', 'teal', 11)",
                new NpgsqlParameter("id", categoryId),
                new NpgsqlParameter("owner", userId));
            await BudgetLimitRows.InsertAsync(_database, userId, categoryId);

            await RejectedInsert.AssertAsync(
                () => _database.ExecuteAsync("DELETE FROM categories WHERE id = @id", new NpgsqlParameter("id", categoryId)),
                RestrictViolation);
        }

        [Fact]
        public async Task Entity_ReadsBackEveryColumnOfARowWithAndWithoutALimit()
        {
            Guid userId = await UserRows.AddAsync(_database);
            Guid categoryRowId = await BudgetLimitRows.InsertAsync(_database, userId, BudgetLimitRows.BuiltInFoodId, new DateOnly(2026, 9, 1), 800_000);
            Guid noLimitRowId = await BudgetLimitRows.InsertAsync(_database, userId, null, new DateOnly(2026, 9, 1), null);

            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            BudgetLimit categoryRow = await context.Set<BudgetLimit>().SingleAsync(row => row.Id == categoryRowId, TestContext.Current.CancellationToken);
            BudgetLimit noLimitRow = await context.Set<BudgetLimit>().SingleAsync(row => row.Id == noLimitRowId, TestContext.Current.CancellationToken);

            Assert.Equal(userId, categoryRow.UserId);
            Assert.Equal(BudgetLimitRows.BuiltInFoodId, categoryRow.CategoryId);
            Assert.Equal(new DateOnly(2026, 9, 1), categoryRow.EffectiveFrom);
            Assert.Equal(800_000, categoryRow.LimitMinor);
            Assert.NotEqual(default, categoryRow.CreatedAt);
            Assert.Null(categoryRow.UpdatedAt);
            Assert.Null(noLimitRow.CategoryId);
            Assert.Null(noLimitRow.LimitMinor);
        }
    }
}
