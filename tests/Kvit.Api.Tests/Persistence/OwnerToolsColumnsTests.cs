using Kvit.Api.Tests.OwnerTools;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class OwnerToolsColumnsTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        private const string ActionCheckPrefix = "ck_admin_actions_";
        private static readonly DateTimeOffset SuspendedAt = new(2026, 11, 8, 9, 30, 15, TimeSpan.Zero);

        [Theory]
        [InlineData("users", "suspended_at")]
        [InlineData("groups", "admin_closed_at")]
        public async Task NewColumn_IsATimestampWithTimeZoneThatIsNullableAndHasNoDatabaseDefault(string table, string column)
        {
            Assert.Equal("timestamp with time zone", await SchemaQueries.DataTypeAsync(_database, table, column));
            Assert.Equal("YES", await SchemaQueries.IsNullableAsync(_database, table, column));
            Assert.Null(await SchemaQueries.DefaultAsync(_database, table, column));
        }

        [Fact]
        public async Task Insert_UserWithoutASuspension_HasSuspendedAtNull()
        {
            Guid userId = await UserRows.AddAsync(_database);

            Assert.Null(await OwnerToolsRows.SuspendedAtAsync(_database, userId));
        }

        [Fact]
        public async Task SuspendedAt_SetAndThenCleared_IsReadBackUnchangedAndThenNull()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await OwnerToolsRows.SetSuspendedAtAsync(_database, userId, SuspendedAt);
            Assert.Equal(SuspendedAt, await OwnerToolsRows.SuspendedAtAsync(_database, userId));
            await OwnerToolsRows.SetSuspendedAtAsync(_database, userId, null);

            Assert.Null(await OwnerToolsRows.SuspendedAtAsync(_database, userId));
        }

        [Fact]
        public async Task Insert_GroupWithoutAnAdminClose_HasAdminClosedAtNull()
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);

            Assert.Null(await OwnerToolsRows.AdminClosedAtAsync(_database, groupId));
        }

        [Fact]
        public async Task AdminClosedAt_SetAndThenCleared_IsReadBackUnchangedAndThenNull()
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);

            await OwnerToolsRows.SetAdminClosedAtAsync(_database, groupId, SuspendedAt);
            Assert.Equal(SuspendedAt, await OwnerToolsRows.AdminClosedAtAsync(_database, groupId));
            await OwnerToolsRows.SetAdminClosedAtAsync(_database, groupId, null);

            Assert.Null(await OwnerToolsRows.AdminClosedAtAsync(_database, groupId));
        }

        [Fact]
        public async Task AdminClosedAt_OnAFinishedGroup_DoesNotChangeItsStatus()
        {
            Guid ownerId = await UserRows.AddAsync(_database);
            Guid groupId = await GroupRows.InsertGroupAsync(_database, ownerId, status: "Finished");

            await OwnerToolsRows.SetAdminClosedAtAsync(_database, groupId, SuspendedAt);

            Assert.Equal("Finished", await GroupRows.GroupValueAsync(_database, groupId, "status"));
        }

        [Fact]
        public async Task AdminActions_HasExactlyTheEightColumns()
        {
            List<string?> columns = await SchemaQueries.ColumnNamesAsync(_database, "admin_actions");

            Assert.Equal(["action", "admin_user_id", "created_at", "id", "reason", "target_group_id", "target_label", "target_user_id"], columns);
        }

        [Theory]
        [InlineData("id", "bigint", "NO")]
        [InlineData("admin_user_id", "uuid", "NO")]
        [InlineData("action", "character varying", "NO")]
        [InlineData("target_user_id", "uuid", "YES")]
        [InlineData("target_group_id", "uuid", "YES")]
        [InlineData("target_label", "character varying", "NO")]
        [InlineData("reason", "character varying", "NO")]
        [InlineData("created_at", "timestamp with time zone", "NO")]
        public async Task AdminActions_Column_HasTheExpectedTypeAndNullability(string column, string expectedDataType, string expectedIsNullable)
        {
            Assert.Equal(expectedDataType, await SchemaQueries.DataTypeAsync(_database, "admin_actions", column));
            Assert.Equal(expectedIsNullable, await SchemaQueries.IsNullableAsync(_database, "admin_actions", column));
        }

        [Theory]
        [InlineData("action", "30")]
        [InlineData("target_label", "120")]
        [InlineData("reason", "500")]
        public async Task AdminActions_TextColumn_HasTheExpectedMaximumLength(string column, string expectedMaxLength)
        {
            Assert.Equal(expectedMaxLength, await SchemaQueries.MaxLengthAsync(_database, "admin_actions", column));
        }

        [Fact]
        public async Task AdminActions_Id_IsAnIdentityColumnAndTheOnlyPrimaryKeyColumn()
        {
            List<string?> primaryKeyColumns = await _database.QueryAsync(
                "SELECT a.attname FROM pg_index i JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY (i.indkey) "
                + "WHERE i.indrelid = 'admin_actions'::regclass AND i.indisprimary");

            Assert.Equal("YES", await SchemaQueries.IsIdentityAsync(_database, "admin_actions", "id"));
            Assert.Equal(["id"], primaryKeyColumns);
        }

        [Fact]
        public async Task AdminActions_TwoRows_GetIncreasingIds()
        {
            Guid adminId = await UserRows.AddAsync(_database);
            Guid targetId = await UserRows.AddAsync(_database);
            await OwnerToolsRows.InsertActionAsync(_database, adminId, "Suspended", targetId, null);
            await OwnerToolsRows.InsertActionAsync(_database, adminId, "Unsuspended", targetId, null);

            List<string?> ids = await _database.QueryAsync("SELECT id::text FROM admin_actions WHERE target_user_id = @id ORDER BY id", new NpgsqlParameter("id", targetId));

            Assert.True(long.Parse(ids[1]!) > long.Parse(ids[0]!));
        }

        [Fact]
        public async Task AdminActions_HasAnIndexOnCreatedAt()
        {
            List<string?> definitions = await _database.QueryAsync("SELECT indexdef FROM pg_indexes WHERE schemaname = 'public' AND tablename = 'admin_actions'");

            Assert.Contains(definitions, definition => definition!.Contains("(created_at", StringComparison.Ordinal));
        }

        [Theory]
        [InlineData("Suspended")]
        [InlineData("Unsuspended")]
        [InlineData("NameErased")]
        public async Task Action_AboutAPerson_IsAccepted(string action)
        {
            Guid adminId = await UserRows.AddAsync(_database);
            Guid targetId = await UserRows.AddAsync(_database);

            await OwnerToolsRows.InsertActionAsync(_database, adminId, action, targetId, null);

            Assert.Equal(action, Assert.Single(await OwnerToolsRows.ActionsAboutAsync(_database, targetId)).Action);
        }

        [Theory]
        [InlineData("GroupClosed")]
        [InlineData("GroupReopened")]
        public async Task Action_AboutAGroup_IsAccepted(string action)
        {
            Guid adminId = await UserRows.AddAsync(_database);
            Guid groupId = Guid.CreateVersion7();

            await OwnerToolsRows.InsertActionAsync(_database, adminId, action, null, groupId);

            Assert.Equal(action, Assert.Single(await OwnerToolsRows.ActionsAboutAsync(_database, groupId)).Action);
        }

        [Theory]
        [InlineData("suspended")]
        [InlineData("SUSPENDED")]
        [InlineData("Banned")]
        [InlineData("")]
        [InlineData("Suspended ")]
        [InlineData("GroupDeleted")]
        public async Task Action_OutsideTheFiveKnownNames_ViolatesACheckConstraint(string action)
        {
            Guid adminId = await UserRows.AddAsync(_database);
            Guid targetId = await UserRows.AddAsync(_database);

            await AssertCheckViolatedAsync(() => OwnerToolsRows.InsertActionAsync(_database, adminId, action, targetId, null));

            Assert.Empty(await OwnerToolsRows.ActionsAboutAsync(_database, targetId));
        }

        [Fact]
        public async Task Action_LongerThan30Characters_IsRejected()
        {
            Guid adminId = await UserRows.AddAsync(_database);
            Guid targetId = await UserRows.AddAsync(_database);

            PostgresException error = await Assert.ThrowsAsync<PostgresException>(
                () => OwnerToolsRows.InsertActionAsync(_database, adminId, new string('S', 31), targetId, null));

            string[] acceptableRejections = [PostgresErrorCodes.StringDataRightTruncation, PostgresErrorCodes.CheckViolation];
            Assert.Contains(error.SqlState, acceptableRejections);
        }

        [Fact]
        public async Task Reason_Empty_ViolatesACheckConstraint()
        {
            Guid adminId = await UserRows.AddAsync(_database);
            Guid targetId = await UserRows.AddAsync(_database);

            await AssertCheckViolatedAsync(() => OwnerToolsRows.InsertActionAsync(_database, adminId, "Suspended", targetId, null, reason: string.Empty));

            Assert.Empty(await OwnerToolsRows.ActionsAboutAsync(_database, targetId));
        }

        [Fact]
        public async Task Reason_Of500Characters_IsAcceptedAndReadBackUnchanged()
        {
            Guid adminId = await UserRows.AddAsync(_database);
            Guid targetId = await UserRows.AddAsync(_database);
            string reason = new('r', 500);

            await OwnerToolsRows.InsertActionAsync(_database, adminId, "Suspended", targetId, null, reason: reason);

            Assert.Equal(reason, Assert.Single(await OwnerToolsRows.ActionsAboutAsync(_database, targetId)).Reason);
        }

        [Fact]
        public async Task Reason_Of501Characters_IsRejected()
        {
            Guid adminId = await UserRows.AddAsync(_database);
            Guid targetId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(
                () => OwnerToolsRows.InsertActionAsync(_database, adminId, "Suspended", targetId, null, reason: new string('r', 501)),
                PostgresErrorCodes.StringDataRightTruncation);
        }

        [Fact]
        public async Task TargetLabel_Of120Characters_IsAcceptedAndOf121IsRejected()
        {
            Guid adminId = await UserRows.AddAsync(_database);
            Guid targetId = await UserRows.AddAsync(_database);

            await OwnerToolsRows.InsertActionAsync(_database, adminId, "Suspended", targetId, null, targetLabel: new string('L', 120));
            await RejectedInsert.AssertAsync(
                () => OwnerToolsRows.InsertActionAsync(_database, adminId, "Suspended", targetId, null, targetLabel: new string('L', 121)),
                PostgresErrorCodes.StringDataRightTruncation);

            Assert.Single(await OwnerToolsRows.ActionsAboutAsync(_database, targetId));
        }

        [Fact]
        public async Task Target_BothAPersonAndAGroup_ViolatesACheckConstraint()
        {
            Guid adminId = await UserRows.AddAsync(_database);
            Guid targetId = await UserRows.AddAsync(_database);
            Guid groupId = Guid.CreateVersion7();

            await AssertCheckViolatedAsync(() => OwnerToolsRows.InsertActionAsync(_database, adminId, "Suspended", targetId, groupId));

            Assert.Empty(await OwnerToolsRows.ActionsAboutAsync(_database, targetId));
        }

        [Fact]
        public async Task Target_NeitherAPersonNorAGroup_ViolatesACheckConstraint()
        {
            Guid adminId = await UserRows.AddAsync(_database);
            int before = await OwnerToolsRows.CountActionsAsync(_database);

            await AssertCheckViolatedAsync(() => OwnerToolsRows.InsertActionAsync(_database, adminId, "Suspended", null, null));

            Assert.Equal(before, await OwnerToolsRows.CountActionsAsync(_database));
        }

        [Fact]
        public async Task CreatedAt_Null_IsRejected()
        {
            Guid adminId = await UserRows.AddAsync(_database);
            Guid targetId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(
                () => _database.ExecuteAsync(
                    "INSERT INTO admin_actions (admin_user_id, action, target_user_id, target_label, reason, created_at) VALUES (@admin, 'Suspended', @target, 'Ana', 'Because', NULL)",
                    new NpgsqlParameter("admin", adminId),
                    new NpgsqlParameter("target", targetId)),
                PostgresErrorCodes.NotNullViolation);
        }

        [Fact]
        public async Task Action_ForAnUnknownAdmin_ViolatesTheForeignKey()
        {
            Guid targetId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(
                () => OwnerToolsRows.InsertActionAsync(_database, Guid.CreateVersion7(), "Suspended", targetId, null),
                PostgresErrorCodes.ForeignKeyViolation);
        }

        [Fact]
        public async Task Action_ForAnUnknownTargetPerson_ViolatesTheForeignKey()
        {
            Guid adminId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(
                () => OwnerToolsRows.InsertActionAsync(_database, adminId, "Suspended", Guid.CreateVersion7(), null),
                PostgresErrorCodes.ForeignKeyViolation);
        }

        [Fact]
        public async Task Action_ForAGroupThatDoesNotExist_IsAcceptedBecauseTheGroupMayBeDeletedLater()
        {
            Guid adminId = await UserRows.AddAsync(_database);
            Guid groupId = Guid.CreateVersion7();

            await OwnerToolsRows.InsertActionAsync(_database, adminId, "GroupClosed", null, groupId);

            Assert.Single(await OwnerToolsRows.ActionsAboutAsync(_database, groupId));
        }

        [Fact]
        public async Task Action_AboutAGroupThatIsHardDeletedLater_StaysInTheTable()
        {
            Guid ownerId = await UserRows.AddAsync(_database);
            Guid adminId = await UserRows.AddAsync(_database);
            Guid groupId = await GroupRows.InsertGroupAsync(_database, ownerId);
            await OwnerToolsRows.InsertActionAsync(_database, adminId, "GroupClosed", null, groupId);

            await _database.ExecuteAsync("DELETE FROM groups WHERE id = @id", new NpgsqlParameter("id", groupId));

            Assert.Single(await OwnerToolsRows.ActionsAboutAsync(_database, groupId));
        }

        [Fact]
        public async Task AdminActions_HasNoForeignKeyOnTargetGroupId()
        {
            List<string?> foreignKeyColumns = await _database.QueryAsync(
                "SELECT a.attname FROM pg_constraint c JOIN pg_attribute a ON a.attrelid = c.conrelid AND a.attnum = ANY (c.conkey) "
                + "WHERE c.conrelid = 'admin_actions'::regclass AND c.contype = 'f' ORDER BY a.attname");

            Assert.Equal(["admin_user_id", "target_user_id"], foreignKeyColumns);
        }

        [Fact]
        public async Task AdminActions_BothForeignKeys_RestrictDeletion()
        {
            List<string?> deleteRules = await _database.QueryAsync(
                "SELECT c.confdeltype::text FROM pg_constraint c WHERE c.conrelid = 'admin_actions'::regclass AND c.contype = 'f'");

            Assert.Equal(2, deleteRules.Count);
            Assert.All(deleteRules, rule => Assert.Equal("r", rule));
        }

        [Fact]
        public async Task User_DeletedWhileAnActionNamesThemAsAdmin_ViolatesTheForeignKey()
        {
            Guid adminId = await UserRows.AddAsync(_database);
            Guid targetId = await UserRows.AddAsync(_database);
            await OwnerToolsRows.InsertActionAsync(_database, adminId, "Suspended", targetId, null);

            await RejectedInsert.AssertAsync(
                () => _database.ExecuteAsync("DELETE FROM users WHERE id = @id", new NpgsqlParameter("id", adminId)),
                PostgresErrorCodes.RestrictViolation);
        }

        [Fact]
        public async Task User_DeletedWhileAnActionNamesThemAsTarget_ViolatesTheForeignKey()
        {
            Guid adminId = await UserRows.AddAsync(_database);
            Guid targetId = await UserRows.AddAsync(_database);
            await OwnerToolsRows.InsertActionAsync(_database, adminId, "Suspended", targetId, null);

            await RejectedInsert.AssertAsync(
                () => _database.ExecuteAsync("DELETE FROM users WHERE id = @id", new NpgsqlParameter("id", targetId)),
                PostgresErrorCodes.RestrictViolation);
        }

        [Fact]
        public async Task Migration_OwnerTools_IsAppliedOnceAfterAccountDeletionAndLeavesNothingPending()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            List<string> applied = [.. await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)];
            IEnumerable<string> pending = await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken);

            Assert.Single(applied, migration => migration.EndsWith("_OwnerTools", StringComparison.Ordinal));
            int accountDeletion = applied.FindIndex(migration => migration.EndsWith("_AccountDeletion", StringComparison.Ordinal));
            int ownerTools = applied.FindIndex(migration => migration.EndsWith("_OwnerTools", StringComparison.Ordinal));
            Assert.True(ownerTools > accountDeletion, $"The OwnerTools migration is at {ownerTools} but AccountDeletion is at {accountDeletion}.");
            Assert.Empty(pending);
        }

        private static async Task AssertCheckViolatedAsync(Func<Task> insert)
        {
            PostgresException error = await Assert.ThrowsAsync<PostgresException>(insert);

            Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
            Assert.StartsWith(ActionCheckPrefix, error.ConstraintName, StringComparison.Ordinal);
        }
    }
}
