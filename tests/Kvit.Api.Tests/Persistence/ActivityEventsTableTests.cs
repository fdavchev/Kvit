using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class ActivityEventsTableTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        [Theory]
        [InlineData("id", "uuid")]
        [InlineData("group_id", "uuid")]
        [InlineData("actor_user_id", "uuid")]
        [InlineData("type", "text")]
        [InlineData("expense_id", "uuid")]
        [InlineData("settlement_id", "uuid")]
        [InlineData("member_id", "uuid")]
        [InlineData("changes", "jsonb")]
        [InlineData("data", "jsonb")]
        [InlineData("created_at", "timestamp with time zone")]
        public async Task Column_HasTheExpectedType(string column, string expectedDataType)
        {
            string? dataType = await SchemaQueries.DataTypeAsync(_database, "activity_events", column);

            Assert.Equal(expectedDataType, dataType);
        }

        [Theory]
        [InlineData("id", "NO")]
        [InlineData("group_id", "NO")]
        [InlineData("actor_user_id", "NO")]
        [InlineData("type", "NO")]
        [InlineData("expense_id", "YES")]
        [InlineData("settlement_id", "YES")]
        [InlineData("member_id", "YES")]
        [InlineData("changes", "YES")]
        [InlineData("data", "YES")]
        [InlineData("created_at", "NO")]
        public async Task Column_HasTheExpectedNullability(string column, string expectedIsNullable)
        {
            string? isNullable = await SchemaQueries.IsNullableAsync(_database, "activity_events", column);

            Assert.Equal(expectedIsNullable, isNullable);
        }

        [Theory]
        [InlineData("GroupCreated")]
        [InlineData("GroupRenamed")]
        [InlineData("GroupSettingsChanged")]
        [InlineData("InviteLinkReset")]
        [InlineData("MemberAdded")]
        [InlineData("MemberJoined")]
        [InlineData("MemberClaimed")]
        [InlineData("ClaimUndone")]
        [InlineData("MemberRemoved")]
        [InlineData("MemberLeft")]
        [InlineData("OwnershipTransferred")]
        [InlineData("MemberLetBackIn")]
        [InlineData("InviteLinkRestored")]
        [InlineData("ExpenseAdded")]
        [InlineData("ExpenseEdited")]
        [InlineData("ExpenseDeleted")]
        [InlineData("ExpenseRestored")]
        [InlineData("SettlementRecorded")]
        [InlineData("SettlementConfirmed")]
        [InlineData("SettlementRejected")]
        [InlineData("SettlementCancelled")]
        [InlineData("SettlementDeleted")]
        [InlineData("ClosingStarted")]
        [InlineData("ClosingConfirmed")]
        [InlineData("ClosingObjected")]
        [InlineData("ClosingCancelled")]
        [InlineData("GroupFinished")]
        [InlineData("GroupReopened")]
        [InlineData("GroupDeleted")]
        [InlineData("GroupRestored")]
        public async Task Type_OfTheKnownNames_IsAccepted(string type)
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);
            Guid actorUserId = await UserRows.AddAsync(_database);

            await GroupRows.InsertActivityEventAsync(_database, groupId, actorUserId, type);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_database, groupId, type));
        }

        [Theory]
        [InlineData("Nonsense")]
        [InlineData("groupcreated")]
        [InlineData("")]
        public async Task Type_OutsideTheKnownNames_ViolatesTheCheckConstraint(string type)
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);
            Guid actorUserId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(
                () => GroupRows.InsertActivityEventAsync(_database, groupId, actorUserId, type),
                PostgresErrorCodes.CheckViolation);
        }

        [Fact]
        public async Task Event_ForAnUnknownGroup_ViolatesTheForeignKey()
        {
            Guid actorUserId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(
                () => GroupRows.InsertActivityEventAsync(_database, Guid.CreateVersion7(), actorUserId, "GroupCreated"),
                PostgresErrorCodes.ForeignKeyViolation);
        }

        [Fact]
        public async Task Event_WithAnExpenseIdAndASettlementIdThatPointAtNothing_IsAccepted()
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);
            Guid actorUserId = await UserRows.AddAsync(_database);

            await GroupRows.InsertActivityEventAsync(_database, groupId, actorUserId, "GroupCreated", Guid.CreateVersion7(), Guid.CreateVersion7());

            Assert.Equal(1, await GroupRows.CountEventsAsync(_database, groupId));
        }

        [Fact]
        public async Task Index_OnGroupIdAndCreatedAt_Exists()
        {
            List<string?> indexColumns = await _database.QueryAsync(
                "SELECT string_agg(a.attname, ',' ORDER BY k.ord) "
                + "FROM pg_index i "
                + "JOIN pg_class c ON c.oid = i.indrelid "
                + "CROSS JOIN LATERAL unnest(i.indkey::int2[]) WITH ORDINALITY AS k(attnum, ord) "
                + "JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = k.attnum "
                + "WHERE c.relname = 'activity_events' "
                + "GROUP BY i.indexrelid");

            Assert.Contains(indexColumns, columns => columns is not null && columns.StartsWith("group_id,created_at", StringComparison.Ordinal));
        }
    }
}
