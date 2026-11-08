using Kvit.Api.Tests.Terms;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class TermsColumnsTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        private const string TogetherConstraint = "ck_users_terms_version_and_time_together";
        private static readonly DateTimeOffset AcceptedAt = new(2026, 11, 4, 9, 30, 15, TimeSpan.Zero);

        [Theory]
        [InlineData("terms_version", "character varying")]
        [InlineData("terms_accepted_at", "timestamp with time zone")]
        public async Task Column_HasTheExpectedType(string column, string expectedDataType)
        {
            string? dataType = await SchemaQueries.DataTypeAsync(_database, "users", column);

            Assert.Equal(expectedDataType, dataType);
        }

        [Theory]
        [InlineData("terms_version")]
        [InlineData("terms_accepted_at")]
        public async Task Column_IsNullableAndHasNoDatabaseDefault(string column)
        {
            Assert.Equal("YES", await SchemaQueries.IsNullableAsync(_database, "users", column));
            Assert.Null(await SchemaQueries.DefaultAsync(_database, "users", column));
        }

        [Fact]
        public async Task TermsVersion_IsLimitedTo20Characters()
        {
            string? maxLength = await SchemaQueries.MaxLengthAsync(_database, "users", "terms_version");

            Assert.Equal("20", maxLength);
        }

        [Fact]
        public async Task Users_HaveNoColumnBeyondTheKnownOnesTheTwoTermsOnesAndTheThreeDeletionOnes()
        {
            string[] expected =
            [
                "access_failed_count",
                "budget_total_currency",
                "concurrency_stamp",
                "created_at",
                "deletion_name_choice",
                "deletion_requested_at",
                "display_name",
                "email",
                "email_confirmed",
                "google_picture_url",
                "id",
                "is_guest",
                "is_time_zone_manual",
                "language",
                "lockout_count",
                "lockout_enabled",
                "lockout_end",
                "must_change_password",
                "normalized_email",
                "normalized_user_name",
                "password_hash",
                "phone_number",
                "phone_number_confirmed",
                "purged_at",
                "security_stamp",
                "terms_accepted_at",
                "terms_version",
                "time_zone",
                "two_factor_enabled",
                "user_name",
            ];

            List<string?> columns = await SchemaQueries.ColumnNamesAsync(_database, "users");

            string?[] sorted = [.. columns.Order(StringComparer.Ordinal)];
            Assert.Equal(expected, sorted);
        }

        [Fact]
        public async Task Insert_UserWithoutTerms_HasBothColumnsNull()
        {
            Guid userId = await UserRows.AddAsync(_database);

            Assert.Equal(StoredTerms.None, await TermsRows.ReadAsync(_database, userId));
        }

        [Fact]
        public async Task Terms_BothSet_AreAcceptedAndReadBackUnchanged()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await TermsRows.SetAsync(_database, userId, TermsRequests.CurrentVersion, AcceptedAt);

            Assert.Equal(new StoredTerms(TermsRequests.CurrentVersion, AcceptedAt), await TermsRows.ReadAsync(_database, userId));
        }

        [Fact]
        public async Task Terms_BothSetBackToNull_AreAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);
            await TermsRows.SetAsync(_database, userId, TermsRequests.CurrentVersion, AcceptedAt);

            await TermsRows.ClearAsync(_database, userId);

            Assert.Equal(StoredTerms.None, await TermsRows.ReadAsync(_database, userId));
        }

        [Fact]
        public async Task Terms_VersionWithoutATime_ViolatesTheTogetherCheckConstraint()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await AssertTogetherConstraintViolatedAsync(() => TermsRows.SetAsync(_database, userId, TermsRequests.CurrentVersion, null));

            Assert.Equal(StoredTerms.None, await TermsRows.ReadAsync(_database, userId));
        }

        [Fact]
        public async Task Terms_TimeWithoutAVersion_ViolatesTheTogetherCheckConstraint()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await AssertTogetherConstraintViolatedAsync(() => TermsRows.SetAsync(_database, userId, null, AcceptedAt));

            Assert.Equal(StoredTerms.None, await TermsRows.ReadAsync(_database, userId));
        }

        [Fact]
        public async Task Terms_EmptyVersionWithATime_ViolatesACheckConstraint()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(() => TermsRows.SetAsync(_database, userId, string.Empty, AcceptedAt), PostgresErrorCodes.CheckViolation);

            Assert.Equal(StoredTerms.None, await TermsRows.ReadAsync(_database, userId));
        }

        [Fact]
        public async Task Terms_RejectedChange_LeavesTheEarlierTermsInPlace()
        {
            Guid userId = await UserRows.AddAsync(_database);
            await TermsRows.SetAsync(_database, userId, TermsRequests.OlderVersion, AcceptedAt);

            await AssertTogetherConstraintViolatedAsync(() => TermsRows.SetAsync(_database, userId, TermsRequests.CurrentVersion, null));

            Assert.Equal(new StoredTerms(TermsRequests.OlderVersion, AcceptedAt), await TermsRows.ReadAsync(_database, userId));
        }

        [Fact]
        public async Task TermsVersion_With20Characters_IsAccepted()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await TermsRows.SetAsync(_database, userId, new string('9', 20), AcceptedAt);

            Assert.Equal(new StoredTerms(new string('9', 20), AcceptedAt), await TermsRows.ReadAsync(_database, userId));
        }

        [Fact]
        public async Task TermsVersion_With21Characters_IsRejected()
        {
            Guid userId = await UserRows.AddAsync(_database);

            PostgresException error = await Assert.ThrowsAsync<PostgresException>(() => TermsRows.SetAsync(_database, userId, new string('9', 21), AcceptedAt));

            string[] acceptableRejections = [PostgresErrorCodes.StringDataRightTruncation, PostgresErrorCodes.CheckViolation];
            Assert.Contains(error.SqlState, acceptableRejections);
            Assert.Equal(StoredTerms.None, await TermsRows.ReadAsync(_database, userId));
        }

        [Fact]
        public async Task Migration_TermsAcceptance_IsAppliedOnceAfterGroupPlanAndLeavesNothingPending()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            List<string> applied = [.. await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)];
            IEnumerable<string> pending = await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken);

            Assert.Single(applied, migration => migration.EndsWith("_TermsAcceptance", StringComparison.Ordinal));
            int groupPlan = applied.FindIndex(migration => migration.EndsWith("_GroupPlan", StringComparison.Ordinal));
            int termsAcceptance = applied.FindIndex(migration => migration.EndsWith("_TermsAcceptance", StringComparison.Ordinal));
            Assert.True(termsAcceptance > groupPlan, $"The TermsAcceptance migration is at {termsAcceptance} but GroupPlan is at {groupPlan}.");
            Assert.Empty(pending);
        }

        private static async Task AssertTogetherConstraintViolatedAsync(Func<Task> change)
        {
            PostgresException error = await Assert.ThrowsAsync<PostgresException>(change);

            Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
            Assert.Equal(TogetherConstraint, error.ConstraintName);
        }
    }
}
