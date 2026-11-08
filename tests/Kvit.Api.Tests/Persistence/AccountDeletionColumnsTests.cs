using Kvit.Api.Tests.AccountDeletion;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class AccountDeletionColumnsTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        private static readonly DateTimeOffset RequestedAt = new(2026, 11, 4, 9, 30, 15, TimeSpan.Zero);
        private static readonly DateTimeOffset PurgedAt = RequestedAt.AddDays(30);

        [Theory]
        [InlineData("deletion_requested_at", "timestamp with time zone")]
        [InlineData("deletion_name_choice", "character varying")]
        [InlineData("purged_at", "timestamp with time zone")]
        public async Task Column_HasTheExpectedType(string column, string expectedDataType)
        {
            string? dataType = await SchemaQueries.DataTypeAsync(_database, "users", column);

            Assert.Equal(expectedDataType, dataType);
        }

        [Theory]
        [InlineData("deletion_requested_at")]
        [InlineData("deletion_name_choice")]
        [InlineData("purged_at")]
        public async Task Column_IsNullableAndHasNoDatabaseDefault(string column)
        {
            Assert.Equal("YES", await SchemaQueries.IsNullableAsync(_database, "users", column));
            Assert.Null(await SchemaQueries.DefaultAsync(_database, "users", column));
        }

        [Fact]
        public async Task DeletionNameChoice_IsLimitedTo20Characters()
        {
            string? maxLength = await SchemaQueries.MaxLengthAsync(_database, "users", "deletion_name_choice");

            Assert.Equal("20", maxLength);
        }

        [Fact]
        public async Task Insert_UserWithoutADeletion_HasAllThreeColumnsNull()
        {
            Guid userId = await UserRows.AddAsync(_database);

            Assert.Equal(StoredDeletion.None, await DeletionRows.ReadAsync(_database, userId));
        }

        [Theory]
        [InlineData("KeepName")]
        [InlineData("FormerMember")]
        public async Task Deletion_RequestedWithAKnownChoice_IsAcceptedAndReadBackUnchanged(string nameChoice)
        {
            Guid userId = await UserRows.AddAsync(_database);

            await DeletionRows.SetAsync(_database, userId, RequestedAt, nameChoice, null);

            Assert.Equal(new StoredDeletion(RequestedAt, nameChoice, null), await DeletionRows.ReadAsync(_database, userId));
        }

        [Fact]
        public async Task Deletion_RequestedAndPurged_IsAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await DeletionRows.SetAsync(_database, userId, RequestedAt, "KeepName", PurgedAt);

            Assert.Equal(new StoredDeletion(RequestedAt, "KeepName", PurgedAt), await DeletionRows.ReadAsync(_database, userId));
        }

        [Fact]
        public async Task Deletion_AllThreeSetBackToNull_IsAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);
            await DeletionRows.SetAsync(_database, userId, RequestedAt, "FormerMember", null);

            await DeletionRows.SetAsync(_database, userId, null, null, null);

            Assert.Equal(StoredDeletion.None, await DeletionRows.ReadAsync(_database, userId));
        }

        [Fact]
        public async Task Deletion_RequestedWithoutAChoice_ViolatesACheckConstraint()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await AssertUsersCheckViolatedAsync(() => DeletionRows.SetAsync(_database, userId, RequestedAt, null, null));

            Assert.Equal(StoredDeletion.None, await DeletionRows.ReadAsync(_database, userId));
        }

        [Fact]
        public async Task Deletion_ChoiceWithoutARequestTime_ViolatesACheckConstraint()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await AssertUsersCheckViolatedAsync(() => DeletionRows.SetAsync(_database, userId, null, "KeepName", null));

            Assert.Equal(StoredDeletion.None, await DeletionRows.ReadAsync(_database, userId));
        }

        [Fact]
        public async Task Deletion_PurgedWithoutARequest_ViolatesACheckConstraint()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await AssertUsersCheckViolatedAsync(() => DeletionRows.SetAsync(_database, userId, null, null, PurgedAt));

            Assert.Equal(StoredDeletion.None, await DeletionRows.ReadAsync(_database, userId));
        }

        [Theory]
        [InlineData("keepname")]
        [InlineData("KEEPNAME")]
        [InlineData("formermember")]
        [InlineData("Other")]
        [InlineData("")]
        public async Task Deletion_UnknownOrDifferentlyCasedChoice_ViolatesACheckConstraint(string nameChoice)
        {
            Guid userId = await UserRows.AddAsync(_database);

            await AssertUsersCheckViolatedAsync(() => DeletionRows.SetAsync(_database, userId, RequestedAt, nameChoice, null));

            Assert.Equal(StoredDeletion.None, await DeletionRows.ReadAsync(_database, userId));
        }

        [Fact]
        public async Task Deletion_ChoiceLongerThan20Characters_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);

            PostgresException error = await Assert.ThrowsAsync<PostgresException>(() => DeletionRows.SetAsync(_database, userId, RequestedAt, new string('K', 21), null));

            string[] acceptableRejections = [PostgresErrorCodes.StringDataRightTruncation, PostgresErrorCodes.CheckViolation];
            Assert.Contains(error.SqlState, acceptableRejections);
            Assert.Equal(StoredDeletion.None, await DeletionRows.ReadAsync(_database, userId));
        }

        [Fact]
        public async Task Deletion_RejectedChange_LeavesTheEarlierRequestInPlace()
        {
            Guid userId = await UserRows.AddAsync(_database);
            await DeletionRows.SetAsync(_database, userId, RequestedAt, "KeepName", null);

            await AssertUsersCheckViolatedAsync(() => DeletionRows.SetAsync(_database, userId, null, "KeepName", PurgedAt));

            Assert.Equal(new StoredDeletion(RequestedAt, "KeepName", null), await DeletionRows.ReadAsync(_database, userId));
        }

        [Fact]
        public async Task Migration_AccountDeletion_IsAppliedOnceAfterTermsAcceptanceAndLeavesNothingPending()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            List<string> applied = [.. await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)];
            IEnumerable<string> pending = await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken);

            Assert.Single(applied, migration => migration.EndsWith("_AccountDeletion", StringComparison.Ordinal));
            int termsAcceptance = applied.FindIndex(migration => migration.EndsWith("_TermsAcceptance", StringComparison.Ordinal));
            int accountDeletion = applied.FindIndex(migration => migration.EndsWith("_AccountDeletion", StringComparison.Ordinal));
            Assert.True(accountDeletion > termsAcceptance, $"The AccountDeletion migration is at {accountDeletion} but TermsAcceptance is at {termsAcceptance}.");
            Assert.Empty(pending);
        }

        private static async Task AssertUsersCheckViolatedAsync(Func<Task> change)
        {
            PostgresException error = await Assert.ThrowsAsync<PostgresException>(change);

            Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
            Assert.StartsWith("ck_users_", error.ConstraintName, StringComparison.Ordinal);
        }
    }
}
