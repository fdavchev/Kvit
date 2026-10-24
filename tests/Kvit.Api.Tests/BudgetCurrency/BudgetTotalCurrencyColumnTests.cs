using Kvit.Api.Tests.Persistence;
using Kvit.Domain.MoneyRules;
using Kvit.Infrastructure.Auth;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.BudgetCurrency
{
    public class BudgetTotalCurrencyColumnTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        private const string CheckConstraint = "ck_users_budget_total_currency";

        [Fact]
        public async Task Column_HasNoDatabaseDefault()
        {
            string? columnDefault = await SchemaQueries.DefaultAsync(_database, "users", "budget_total_currency");

            Assert.Null(columnDefault);
        }

        [Fact]
        public async Task CheckConstraints_IncludeTheBudgetTotalCurrencyRule()
        {
            List<string?> names = await _database.QueryAsync("SELECT conname FROM pg_constraint WHERE conrelid = 'users'::regclass AND contype = 'c'");

            Assert.Contains(CheckConstraint, names);
        }

        [Fact]
        public async Task User_SavedWithoutTheCurrency_StoresNull()
        {
            Guid userId = await UserRows.AddAsync(_database);

            Assert.Null(await BudgetCurrencySteps.StoredAsync(_database, userId));
        }

        [Theory]
        [InlineData("MKD")]
        [InlineData("EUR")]
        public async Task Update_MkdOrEur_IsAccepted(string currency)
        {
            Guid userId = await UserRows.AddAsync(_database);

            await _database.ExecuteAsync(
                "UPDATE users SET budget_total_currency = @currency WHERE id = @id",
                new NpgsqlParameter("currency", currency),
                new NpgsqlParameter("id", userId));

            Assert.Equal(currency, await BudgetCurrencySteps.StoredAsync(_database, userId));
        }

        [Fact]
        public async Task Update_BackToNull_IsAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);
            await _database.ExecuteAsync("UPDATE users SET budget_total_currency = 'EUR' WHERE id = @id", new NpgsqlParameter("id", userId));

            await _database.ExecuteAsync("UPDATE users SET budget_total_currency = NULL WHERE id = @id", new NpgsqlParameter("id", userId));

            Assert.Null(await BudgetCurrencySteps.StoredAsync(_database, userId));
        }

        [Theory]
        [InlineData("USD")]
        [InlineData("mkd")]
        [InlineData("eur")]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("MKD ")]
        public async Task Update_AnythingElse_ViolatesTheCheckConstraint(string currency)
        {
            Guid userId = await UserRows.AddAsync(_database);

            PostgresException error = await Assert.ThrowsAsync<PostgresException>(() => _database.ExecuteAsync(
                "UPDATE users SET budget_total_currency = @currency WHERE id = @id",
                new NpgsqlParameter("currency", currency),
                new NpgsqlParameter("id", userId)));

            Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
            Assert.Equal(CheckConstraint, error.ConstraintName);
            Assert.Null(await BudgetCurrencySteps.StoredAsync(_database, userId));
        }

        [Fact]
        public async Task Insert_UnknownCurrency_ViolatesTheCheckConstraint()
        {
            PostgresException error = await Assert.ThrowsAsync<PostgresException>(() => _database.ExecuteAsync(
                "INSERT INTO users (id, display_name, language, time_zone, created_at, access_failed_count, email_confirmed, lockout_enabled, phone_number_confirmed, two_factor_enabled, budget_total_currency) "
                + "VALUES (@id, 'Ana', 'en', 'Europe/Skopje', now(), 0, false, false, false, false, 'USD')",
                new NpgsqlParameter("id", Guid.CreateVersion7())));

            Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
            Assert.Equal(CheckConstraint, error.ConstraintName);
        }

        [Theory]
        [InlineData(Currency.MKD, "MKD")]
        [InlineData(Currency.EUR, "EUR")]
        public async Task User_SavedWithACurrency_StoresItsNameAndReadsItBack(Currency currency, string expectedText)
        {
            Guid userId;
            using (IServiceScope writeScope = _database.CreateScope())
            {
                AppDbContext writeContext = writeScope.ServiceProvider.GetRequiredService<AppDbContext>();
                AppUser user = TestUsers.Create();
                user.SetBudgetTotalCurrency(currency);
                writeContext.Users.Add(user);
                await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                userId = user.Id;
            }

            using IServiceScope readScope = _database.CreateScope();
            AppDbContext readContext = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
            AppUser stored = await readContext.Users.SingleAsync(row => row.Id == userId, TestContext.Current.CancellationToken);

            Assert.Equal(expectedText, await BudgetCurrencySteps.StoredAsync(_database, userId));
            Assert.Equal(currency, stored.BudgetTotalCurrency);
        }

        [Fact]
        public async Task User_ClearedAfterBeingSet_StoresNullAndReadsBackNull()
        {
            Guid userId;
            using (IServiceScope writeScope = _database.CreateScope())
            {
                AppDbContext writeContext = writeScope.ServiceProvider.GetRequiredService<AppDbContext>();
                AppUser user = TestUsers.Create();
                user.SetBudgetTotalCurrency(Currency.EUR);
                writeContext.Users.Add(user);
                await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                user.SetBudgetTotalCurrency(null);
                await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                userId = user.Id;
            }

            using IServiceScope readScope = _database.CreateScope();
            AppDbContext readContext = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
            AppUser stored = await readContext.Users.SingleAsync(row => row.Id == userId, TestContext.Current.CancellationToken);

            Assert.Null(await BudgetCurrencySteps.StoredAsync(_database, userId));
            Assert.Null(stored.BudgetTotalCurrency);
        }
    }
}
