using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class GroupsTableTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        [Theory]
        [InlineData("id", "uuid")]
        [InlineData("kind", "text")]
        [InlineData("name", "character varying")]
        [InlineData("emoji", "text")]
        [InlineData("default_currency", "text")]
        [InlineData("status", "text")]
        [InlineData("owner_user_id", "uuid")]
        [InlineData("closing_started_at", "timestamp with time zone")]
        [InlineData("finished_at", "timestamp with time zone")]
        [InlineData("invite_token", "text")]
        [InlineData("invite_token_created_at", "timestamp with time zone")]
        [InlineData("previous_invite_token", "text")]
        [InlineData("created_by_user_id", "uuid")]
        [InlineData("created_at", "timestamp with time zone")]
        [InlineData("deleted_at", "timestamp with time zone")]
        [InlineData("deleted_by_user_id", "uuid")]
        public async Task Column_HasTheExpectedType(string column, string expectedDataType)
        {
            string? dataType = await SchemaQueries.DataTypeAsync(_database, "groups", column);

            Assert.Equal(expectedDataType, dataType);
        }

        [Theory]
        [InlineData("id", "NO")]
        [InlineData("kind", "NO")]
        [InlineData("name", "NO")]
        [InlineData("emoji", "NO")]
        [InlineData("default_currency", "NO")]
        [InlineData("status", "NO")]
        [InlineData("owner_user_id", "NO")]
        [InlineData("closing_started_at", "YES")]
        [InlineData("finished_at", "YES")]
        [InlineData("invite_token", "NO")]
        [InlineData("invite_token_created_at", "NO")]
        [InlineData("previous_invite_token", "YES")]
        [InlineData("created_by_user_id", "NO")]
        [InlineData("created_at", "NO")]
        [InlineData("deleted_at", "YES")]
        [InlineData("deleted_by_user_id", "YES")]
        public async Task Column_HasTheExpectedNullability(string column, string expectedIsNullable)
        {
            string? isNullable = await SchemaQueries.IsNullableAsync(_database, "groups", column);

            Assert.Equal(expectedIsNullable, isNullable);
        }

        [Fact]
        public async Task Name_IsLimitedTo60Characters()
        {
            string? maxLength = await SchemaQueries.MaxLengthAsync(_database, "groups", "name");

            Assert.Equal("60", maxLength);
        }

        [Fact]
        public async Task Name_Of61Characters_IsRejected()
        {
            Guid ownerUserId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(
                () => GroupRows.InsertGroupAsync(_database, ownerUserId, name: new string('a', 61)),
                PostgresErrorCodes.StringDataRightTruncation);
        }

        [Fact]
        public async Task Name_Empty_ViolatesTheCheckConstraint()
        {
            Guid ownerUserId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(
                () => GroupRows.InsertGroupAsync(_database, ownerUserId, name: ""),
                PostgresErrorCodes.CheckViolation);
        }

        [Theory]
        [InlineData("OneBill")]
        [InlineData("Group")]
        public async Task Kind_OfTheTwoKnownNames_IsAccepted(string kind)
        {
            Guid ownerUserId = await UserRows.AddAsync(_database);

            Guid groupId = await GroupRows.InsertGroupAsync(_database, ownerUserId, kind: kind);

            Assert.Equal(kind, await GroupRows.GroupValueAsync(_database, groupId, "kind"));
        }

        [Theory]
        [InlineData("Bogus")]
        [InlineData("group")]
        [InlineData("")]
        public async Task Kind_OutsideTheTwoKnownNames_ViolatesTheCheckConstraint(string kind)
        {
            Guid ownerUserId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(
                () => GroupRows.InsertGroupAsync(_database, ownerUserId, kind: kind),
                PostgresErrorCodes.CheckViolation);
        }

        [Theory]
        [InlineData("Open")]
        [InlineData("Closing")]
        [InlineData("Finished")]
        public async Task Status_OfTheThreeKnownNames_IsAccepted(string status)
        {
            Guid ownerUserId = await UserRows.AddAsync(_database);

            Guid groupId = await GroupRows.InsertGroupAsync(_database, ownerUserId, status: status);

            Assert.Equal(status, await GroupRows.GroupValueAsync(_database, groupId, "status"));
        }

        [Theory]
        [InlineData("Bogus")]
        [InlineData("open")]
        [InlineData("")]
        public async Task Status_OutsideTheThreeKnownNames_ViolatesTheCheckConstraint(string status)
        {
            Guid ownerUserId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(
                () => GroupRows.InsertGroupAsync(_database, ownerUserId, status: status),
                PostgresErrorCodes.CheckViolation);
        }

        [Theory]
        [InlineData("MKD")]
        [InlineData("EUR")]
        public async Task DefaultCurrency_OfTheTwoKnownNames_IsAccepted(string currency)
        {
            Guid ownerUserId = await UserRows.AddAsync(_database);

            Guid groupId = await GroupRows.InsertGroupAsync(_database, ownerUserId, currency: currency);

            Assert.Equal(currency, await GroupRows.GroupValueAsync(_database, groupId, "default_currency"));
        }

        [Theory]
        [InlineData("USD")]
        [InlineData("mkd")]
        [InlineData("")]
        public async Task DefaultCurrency_OutsideTheTwoKnownNames_ViolatesTheCheckConstraint(string currency)
        {
            Guid ownerUserId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(
                () => GroupRows.InsertGroupAsync(_database, ownerUserId, currency: currency),
                PostgresErrorCodes.CheckViolation);
        }

        [Fact]
        public async Task InviteToken_UsedByTwoGroups_ViolatesTheUniqueIndex()
        {
            Guid ownerUserId = await UserRows.AddAsync(_database);
            string inviteToken = $"shared-{Guid.NewGuid():N}";
            await GroupRows.InsertGroupAsync(_database, ownerUserId, inviteToken: inviteToken);

            await RejectedInsert.AssertAsync(
                () => GroupRows.InsertGroupAsync(_database, ownerUserId, inviteToken: inviteToken),
                PostgresErrorCodes.UniqueViolation);
        }

        [Fact]
        public async Task Group_ForAnUnknownOwner_ViolatesTheForeignKey()
        {
            await RejectedInsert.AssertAsync(
                () => GroupRows.InsertGroupAsync(_database, Guid.CreateVersion7()),
                PostgresErrorCodes.ForeignKeyViolation);
        }

        [Fact]
        public async Task Migration_Groups_IsInTheMigrationHistory()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            IEnumerable<string> applied = await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);

            Assert.Single(applied, migration => migration.EndsWith("_Groups", StringComparison.Ordinal));
        }
    }
}
