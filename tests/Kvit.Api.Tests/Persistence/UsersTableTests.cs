using Kvit.Infrastructure.Auth;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class UsersTableTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        [Theory]
        [InlineData("id", "uuid")]
        [InlineData("display_name", "character varying")]
        [InlineData("language", "text")]
        [InlineData("time_zone", "text")]
        [InlineData("is_time_zone_manual", "boolean")]
        [InlineData("google_picture_url", "text")]
        [InlineData("created_at", "timestamp with time zone")]
        [InlineData("lockout_count", "integer")]
        [InlineData("must_change_password", "boolean")]
        public async Task Column_HasTheExpectedType(string column, string expectedDataType)
        {
            string? dataType = await SchemaQueries.DataTypeAsync(_database, "users", column);

            Assert.Equal(expectedDataType, dataType);
        }

        [Theory]
        [InlineData("display_name", "NO")]
        [InlineData("language", "NO")]
        [InlineData("time_zone", "NO")]
        [InlineData("is_time_zone_manual", "NO")]
        [InlineData("google_picture_url", "YES")]
        [InlineData("created_at", "NO")]
        [InlineData("lockout_count", "NO")]
        [InlineData("must_change_password", "NO")]
        public async Task Column_HasTheExpectedNullability(string column, string expectedIsNullable)
        {
            string? isNullable = await SchemaQueries.IsNullableAsync(_database, "users", column);

            Assert.Equal(expectedIsNullable, isNullable);
        }

        [Fact]
        public async Task DisplayName_IsLimitedTo60Characters()
        {
            string? maxLength = await SchemaQueries.MaxLengthAsync(_database, "users", "display_name");

            Assert.Equal("60", maxLength);
        }

        [Theory]
        [InlineData("is_time_zone_manual", "false")]
        [InlineData("lockout_count", "0")]
        [InlineData("must_change_password", "false")]
        public async Task Column_HasTheExpectedDefault(string column, string expectedDefault)
        {
            string? columnDefault = await SchemaQueries.DefaultAsync(_database, "users", column);

            Assert.Equal(expectedDefault, columnDefault);
        }

        [Theory]
        [InlineData("user_name")]
        [InlineData("normalized_user_name")]
        [InlineData("email")]
        [InlineData("normalized_email")]
        [InlineData("email_confirmed")]
        [InlineData("password_hash")]
        [InlineData("security_stamp")]
        [InlineData("concurrency_stamp")]
        [InlineData("phone_number")]
        [InlineData("phone_number_confirmed")]
        [InlineData("two_factor_enabled")]
        [InlineData("lockout_end")]
        [InlineData("lockout_enabled")]
        [InlineData("access_failed_count")]
        public async Task IdentityColumn_ExistsInSnakeCase(string column)
        {
            List<string?> columns = await SchemaQueries.ColumnNamesAsync(_database, "users");

            Assert.Contains(column, columns);
        }

        [Theory]
        [InlineData("en")]
        [InlineData("mk")]
        public async Task Language_EnglishOrMacedonian_IsAccepted(string language)
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            AppUser user = TestUsers.Create();
            user.Language = language;
            context.Users.Add(user);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            List<string?> stored = await _database.QueryAsync("SELECT language FROM users WHERE id = @id", new NpgsqlParameter("id", user.Id));
            Assert.Equal(language, Assert.Single(stored));
        }

        [Theory]
        [InlineData("de")]
        [InlineData("EN")]
        [InlineData("")]
        public async Task Language_AnythingElse_ViolatesTheCheckConstraint(string language)
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            AppUser user = TestUsers.Create();
            user.Language = language;
            context.Users.Add(user);

            PostgresException error = await PostgresErrors.SaveChangesFailingAsync(context);

            Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        }

        [Fact]
        public async Task DisplayName_With60Characters_IsAccepted()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            AppUser user = TestUsers.Create();
            user.DisplayName = new string('a', 60);
            context.Users.Add(user);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            List<string?> stored = await _database.QueryAsync("SELECT display_name FROM users WHERE id = @id", new NpgsqlParameter("id", user.Id));
            Assert.Equal(new string('a', 60), Assert.Single(stored));
        }

        [Fact]
        public async Task DisplayName_With61Characters_IsRejected()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            AppUser user = TestUsers.Create();
            user.DisplayName = new string('a', 61);
            context.Users.Add(user);

            PostgresException error = await PostgresErrors.SaveChangesFailingAsync(context);

            Assert.Equal(PostgresErrorCodes.StringDataRightTruncation, error.SqlState);
        }

        [Fact]
        public async Task DisplayName_Empty_ViolatesTheCheckConstraint()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            AppUser user = TestUsers.Create();
            user.DisplayName = string.Empty;
            context.Users.Add(user);

            PostgresException error = await PostgresErrors.SaveChangesFailingAsync(context);

            Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        }

        [Fact]
        public async Task TimeZone_Empty_ViolatesTheCheckConstraint()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            AppUser user = TestUsers.Create();
            user.TimeZone = string.Empty;
            context.Users.Add(user);

            PostgresException error = await PostgresErrors.SaveChangesFailingAsync(context);

            Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        }

        [Fact]
        public async Task CreatedAt_DefaultValue_ViolatesTheCheckConstraint()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            AppUser user = TestUsers.Create();
            user.CreatedAt = new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.Zero);
            context.Users.Add(user);

            PostgresException error = await PostgresErrors.SaveChangesFailingAsync(context);

            Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        }

        [Fact]
        public async Task User_SavedWithEveryKvitColumn_ReadsBackUnchanged()
        {
            DateTimeOffset createdAt = new(2026, 9, 29, 10, 15, 30, TimeSpan.Zero);
            Guid userId;
            using (IServiceScope writeScope = _database.CreateScope())
            {
                AppDbContext writeContext = writeScope.ServiceProvider.GetRequiredService<AppDbContext>();
                AppUser user = TestUsers.Create();
                user.DisplayName = "Филип";
                user.Language = "mk";
                user.TimeZone = "Europe/Berlin";
                user.IsTimeZoneManual = true;
                user.GooglePictureUrl = "https://example.com/picture.jpg";
                user.CreatedAt = createdAt;
                user.LockoutCount = 2;
                writeContext.Users.Add(user);
                await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                userId = user.Id;
            }

            using IServiceScope readScope = _database.CreateScope();
            AppDbContext readContext = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
            AppUser stored = await readContext.Users.SingleAsync(row => row.Id == userId, TestContext.Current.CancellationToken);

            Assert.Equal("Филип", stored.DisplayName);
            Assert.Equal("mk", stored.Language);
            Assert.Equal("Europe/Berlin", stored.TimeZone);
            Assert.True(stored.IsTimeZoneManual);
            Assert.Equal("https://example.com/picture.jpg", stored.GooglePictureUrl);
            Assert.Equal(createdAt, stored.CreatedAt);
            Assert.Equal(2, stored.LockoutCount);
        }

        [Fact]
        public async Task User_WithoutGooglePicture_StoresNull()
        {
            Guid userId;
            using (IServiceScope writeScope = _database.CreateScope())
            {
                AppDbContext writeContext = writeScope.ServiceProvider.GetRequiredService<AppDbContext>();
                AppUser user = TestUsers.Create();
                writeContext.Users.Add(user);
                await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                userId = user.Id;
            }

            using IServiceScope readScope = _database.CreateScope();
            AppDbContext readContext = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
            AppUser stored = await readContext.Users.SingleAsync(row => row.Id == userId, TestContext.Current.CancellationToken);

            Assert.Null(stored.GooglePictureUrl);
            Assert.False(stored.IsTimeZoneManual);
            Assert.Equal(0, stored.LockoutCount);
        }
    }
}
