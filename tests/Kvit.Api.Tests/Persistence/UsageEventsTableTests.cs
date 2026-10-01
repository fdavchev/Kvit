using Kvit.Domain.Entities;
using Kvit.Infrastructure.Auth;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class UsageEventsTableTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        private static readonly DateOnly SomeDay = new(2026, 9, 29);
        private static readonly DateOnly NextDay = new(2026, 9, 30);

        [Theory]
        [InlineData("id", "bigint")]
        [InlineData("user_id", "uuid")]
        [InlineData("type", "text")]
        [InlineData("detail", "text")]
        [InlineData("occurred_at", "timestamp with time zone")]
        [InlineData("occurred_on", "date")]
        public async Task Column_HasTheExpectedType(string column, string expectedDataType)
        {
            string? dataType = await SchemaQueries.DataTypeAsync(_database, "usage_events", column);

            Assert.Equal(expectedDataType, dataType);
        }

        [Theory]
        [InlineData("id", "NO")]
        [InlineData("user_id", "YES")]
        [InlineData("type", "NO")]
        [InlineData("detail", "YES")]
        [InlineData("occurred_at", "NO")]
        [InlineData("occurred_on", "NO")]
        public async Task Column_HasTheExpectedNullability(string column, string expectedIsNullable)
        {
            string? isNullable = await SchemaQueries.IsNullableAsync(_database, "usage_events", column);

            Assert.Equal(expectedIsNullable, isNullable);
        }

        [Fact]
        public async Task Id_IsAnIdentityColumn()
        {
            string? isIdentity = await SchemaQueries.IsIdentityAsync(_database, "usage_events", "id");

            Assert.Equal("YES", isIdentity);
        }

        [Fact]
        public async Task Events_SavedWithoutIds_GetDistinctGeneratedIds()
        {
            Guid userId = await AddUserAsync();
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            UsageEvent first = UsageEvent.SignedUp(userId, SignUpMethod.Email, DateTimeOffset.UtcNow);
            UsageEvent second = UsageEvent.SignedUp(userId, SignUpMethod.Google, DateTimeOffset.UtcNow);
            context.UsageEvents.AddRange(first, second);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            Assert.True(first.Id > 0);
            Assert.True(second.Id > 0);
            Assert.NotEqual(first.Id, second.Id);
        }

        [Fact]
        public async Task SignedUpEvent_ReadsBackUnchanged()
        {
            Guid userId = await AddUserAsync();
            DateTimeOffset occurredAt = new(2026, 9, 29, 23, 30, 0, TimeSpan.FromHours(-5));
            long eventId;
            using (IServiceScope writeScope = _database.CreateScope())
            {
                AppDbContext writeContext = writeScope.ServiceProvider.GetRequiredService<AppDbContext>();
                UsageEvent usageEvent = UsageEvent.SignedUp(userId, SignUpMethod.Google, occurredAt);
                writeContext.UsageEvents.Add(usageEvent);
                await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                eventId = usageEvent.Id;
            }

            using IServiceScope readScope = _database.CreateScope();
            AppDbContext readContext = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
            UsageEvent stored = await readContext.UsageEvents.SingleAsync(row => row.Id == eventId, TestContext.Current.CancellationToken);

            Assert.Equal(userId, stored.UserId);
            Assert.Equal(UsageEventType.SignedUp, stored.Type);
            Assert.Equal("Google", stored.Detail);
            Assert.Equal(occurredAt, stored.OccurredAt);
            Assert.Equal(TimeSpan.Zero, stored.OccurredAt.Offset);
            Assert.Equal(new DateOnly(2026, 9, 30), stored.OccurredOn);
        }

        [Fact]
        public async Task SignedUpEvent_IsStoredAsText()
        {
            Guid userId = await AddUserAsync();
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            UsageEvent usageEvent = UsageEvent.SignedUp(userId, SignUpMethod.Email, new DateTimeOffset(2026, 9, 29, 23, 30, 0, TimeSpan.FromHours(-5)));
            context.UsageEvents.Add(usageEvent);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            List<string?> stored = await _database.QueryAsync(
                "SELECT type || '|' || detail || '|' || occurred_on::text FROM usage_events WHERE id = @id",
                new NpgsqlParameter("id", usageEvent.Id));

            Assert.Equal("SignedUp|Email|2026-09-30", Assert.Single(stored));
        }

        [Fact]
        public async Task ForeignKeyToUsers_DeletesWithSetNull()
        {
            List<string?> deleteActions = await _database.QueryAsync(
                "SELECT confdeltype::text FROM pg_constraint WHERE contype = 'f' AND conrelid = 'public.usage_events'::regclass AND confrelid = 'public.users'::regclass");

            Assert.Equal("n", Assert.Single(deleteActions));
        }

        [Fact]
        public async Task Event_ForAnUnknownUser_ViolatesTheForeignKey()
        {
            PostgresException error = await Assert.ThrowsAsync<PostgresException>(
                () => InsertEventAsync(Guid.CreateVersion7(), "SignedUp", SomeDay));

            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, error.SqlState);
        }

        [Fact]
        public async Task DeletingTheUser_KeepsTheEventWithUserIdNull()
        {
            Guid userId = await AddUserAsync();
            long eventId;
            using (IServiceScope writeScope = _database.CreateScope())
            {
                AppDbContext writeContext = writeScope.ServiceProvider.GetRequiredService<AppDbContext>();
                UsageEvent usageEvent = UsageEvent.SignedUp(userId, SignUpMethod.Email, DateTimeOffset.UtcNow);
                writeContext.UsageEvents.Add(usageEvent);
                await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                eventId = usageEvent.Id;
            }
            using (IServiceScope deleteScope = _database.CreateScope())
            {
                AppDbContext deleteContext = deleteScope.ServiceProvider.GetRequiredService<AppDbContext>();
                await deleteContext.Users.Where(row => row.Id == userId).ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            }

            using IServiceScope readScope = _database.CreateScope();
            AppDbContext readContext = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
            UsageEvent stored = await readContext.UsageEvents.SingleAsync(row => row.Id == eventId, TestContext.Current.CancellationToken);

            Assert.Null(stored.UserId);
            Assert.Equal(UsageEventType.SignedUp, stored.Type);
        }

        [Theory]
        [InlineData("SignedUp")]
        [InlineData("Active")]
        [InlineData("JoinedViaInvite")]
        [InlineData("GroupCreated")]
        [InlineData("ExpenseAdded")]
        [InlineData("SettlementConfirmed")]
        public async Task Type_OfTheSixKnownNames_IsAccepted(string type)
        {
            Guid userId = await AddUserAsync();

            await InsertEventAsync(userId, type, SomeDay);

            List<string?> stored = await _database.QueryAsync(
                "SELECT type FROM usage_events WHERE user_id = @user_id",
                new NpgsqlParameter("user_id", userId));
            Assert.Equal(type, Assert.Single(stored));
        }

        [Theory]
        [InlineData("Bogus")]
        [InlineData("active")]
        [InlineData("")]
        public async Task Type_OutsideTheSixKnownNames_ViolatesTheCheckConstraint(string type)
        {
            Guid userId = await AddUserAsync();

            PostgresException error = await Assert.ThrowsAsync<PostgresException>(
                () => InsertEventAsync(userId, type, SomeDay));

            Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        }

        [Fact]
        public async Task ActiveEvent_TwiceForTheSameUserAndDay_ViolatesTheUniqueIndex()
        {
            Guid userId = await AddUserAsync();
            await InsertEventAsync(userId, "Active", SomeDay);

            PostgresException error = await Assert.ThrowsAsync<PostgresException>(
                () => InsertEventAsync(userId, "Active", SomeDay));

            Assert.Equal(PostgresErrorCodes.UniqueViolation, error.SqlState);
        }

        [Fact]
        public async Task ActiveEvent_OnDifferentDaysForTheSameUser_IsAllowed()
        {
            Guid userId = await AddUserAsync();
            await InsertEventAsync(userId, "Active", SomeDay);

            await InsertEventAsync(userId, "Active", NextDay);

            Assert.Equal(2, await CountEventsAsync(userId));
        }

        [Fact]
        public async Task ActiveEvent_OnTheSameDayForDifferentUsers_IsAllowed()
        {
            Guid firstUserId = await AddUserAsync();
            Guid secondUserId = await AddUserAsync();
            await InsertEventAsync(firstUserId, "Active", SomeDay);

            await InsertEventAsync(secondUserId, "Active", SomeDay);

            Assert.Equal(1, await CountEventsAsync(secondUserId));
        }

        [Fact]
        public async Task SignedUpEvent_TwiceForTheSameUserAndDay_IsAllowed()
        {
            Guid userId = await AddUserAsync();
            await InsertEventAsync(userId, "SignedUp", SomeDay);

            await InsertEventAsync(userId, "SignedUp", SomeDay);

            Assert.Equal(2, await CountEventsAsync(userId));
        }

        [Fact]
        public async Task ActiveAndSignedUpEvents_ForTheSameUserAndDay_AreAllowed()
        {
            Guid userId = await AddUserAsync();
            await InsertEventAsync(userId, "Active", SomeDay);

            await InsertEventAsync(userId, "SignedUp", SomeDay);

            Assert.Equal(2, await CountEventsAsync(userId));
        }

        [Fact]
        public async Task ActiveEvent_WithoutAUserTwiceOnTheSameDay_IsAllowed()
        {
            List<string?> before = await _database.QueryAsync("SELECT count(*)::text FROM usage_events WHERE user_id IS NULL");

            await InsertEventAsync(null, "Active", SomeDay);
            await InsertEventAsync(null, "Active", SomeDay);

            List<string?> after = await _database.QueryAsync("SELECT count(*)::text FROM usage_events WHERE user_id IS NULL");
            Assert.Equal(int.Parse(Assert.Single(before)!) + 2, int.Parse(Assert.Single(after)!));
        }

        private async Task<Guid> AddUserAsync()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            AppUser user = TestUsers.Create();
            context.Users.Add(user);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            return user.Id;
        }

        private Task InsertEventAsync(Guid? userId, string type, DateOnly occurredOn)
        {
            object userIdValue = userId is null ? DBNull.Value : userId.Value;

            return _database.ExecuteAsync(
                "INSERT INTO usage_events (user_id, type, occurred_at, occurred_on) VALUES (@user_id, @type, now(), @occurred_on)",
                new NpgsqlParameter("user_id", NpgsqlDbType.Uuid) { Value = userIdValue },
                new NpgsqlParameter("type", type),
                new NpgsqlParameter("occurred_on", occurredOn));
        }

        private async Task<int> CountEventsAsync(Guid userId)
        {
            List<string?> counts = await _database.QueryAsync(
                "SELECT count(*)::text FROM usage_events WHERE user_id = @user_id",
                new NpgsqlParameter("user_id", userId));

            return int.Parse(Assert.Single(counts)!);
        }
    }
}
