using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class SettlementsTableTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        [Theory]
        [InlineData("id", "uuid")]
        [InlineData("group_id", "uuid")]
        [InlineData("from_member_id", "uuid")]
        [InlineData("to_member_id", "uuid")]
        [InlineData("amount_minor", "bigint")]
        [InlineData("currency", "text")]
        [InlineData("status", "text")]
        [InlineData("recorded_by_user_id", "uuid")]
        [InlineData("created_at", "timestamp with time zone")]
        [InlineData("resolved_by_user_id", "uuid")]
        [InlineData("resolved_at", "timestamp with time zone")]
        [InlineData("deleted_at", "timestamp with time zone")]
        [InlineData("deleted_by_user_id", "uuid")]
        [InlineData("client_request_id", "uuid")]
        public async Task Column_HasTheExpectedType(string column, string expectedDataType)
        {
            string? dataType = await SchemaQueries.DataTypeAsync(_database, "settlements", column);

            Assert.Equal(expectedDataType, dataType);
        }

        [Theory]
        [InlineData("id", "NO")]
        [InlineData("group_id", "NO")]
        [InlineData("from_member_id", "NO")]
        [InlineData("to_member_id", "NO")]
        [InlineData("amount_minor", "NO")]
        [InlineData("currency", "NO")]
        [InlineData("status", "NO")]
        [InlineData("recorded_by_user_id", "NO")]
        [InlineData("created_at", "NO")]
        [InlineData("resolved_by_user_id", "YES")]
        [InlineData("resolved_at", "YES")]
        [InlineData("deleted_at", "YES")]
        [InlineData("deleted_by_user_id", "YES")]
        [InlineData("client_request_id", "NO")]
        public async Task Column_HasTheExpectedNullability(string column, string expectedIsNullable)
        {
            string? isNullable = await SchemaQueries.IsNullableAsync(_database, "settlements", column);

            Assert.Equal(expectedIsNullable, isNullable);
        }

        [Fact]
        public async Task Table_HasExactlyTheFourteenColumns()
        {
            List<string?> columns = await SchemaQueries.ColumnNamesAsync(_database, "settlements");

            string[] expected =
            [
                "amount_minor",
                "client_request_id",
                "created_at",
                "currency",
                "deleted_at",
                "deleted_by_user_id",
                "from_member_id",
                "group_id",
                "id",
                "recorded_by_user_id",
                "resolved_at",
                "resolved_by_user_id",
                "status",
                "to_member_id",
            ];
            Assert.Equal(expected, columns);
        }

        [Fact]
        public async Task Id_IsThePrimaryKey()
        {
            List<string?> primaryKeyColumns = await _database.QueryAsync(
                "SELECT a.attname FROM pg_index i JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY (i.indkey) "
                + "WHERE i.indrelid = 'settlements'::regclass AND i.indisprimary");

            Assert.Equal("id", Assert.Single(primaryKeyColumns));
        }

        [Fact]
        public async Task CheckConstraints_AreExactlyTheFourNamedOnes()
        {
            List<string?> names = await _database.QueryAsync(
                "SELECT conname FROM pg_constraint WHERE conrelid = 'settlements'::regclass AND contype = 'c' ORDER BY conname");

            string[] expected =
            [
                "ck_settlements_amount_minor_positive",
                "ck_settlements_currency",
                "ck_settlements_different_members",
                "ck_settlements_status",
            ];
            Assert.Equal(expected, names);
        }

        [Fact]
        public async Task ForeignKeys_AreExactlyTheThreeNamedOnes()
        {
            List<string?> names = await _database.QueryAsync(
                "SELECT conname FROM pg_constraint WHERE conrelid = 'settlements'::regclass AND contype = 'f' ORDER BY conname");

            string[] expected =
            [
                "fk_settlements_group_members_from_member_id",
                "fk_settlements_group_members_to_member_id",
                "fk_settlements_groups_group_id",
            ];
            Assert.Equal(expected, names);
        }

        [Theory]
        [InlineData("ix_settlements_group_id", "group_id")]
        [InlineData("ix_settlements_from_member_id", "from_member_id")]
        [InlineData("ix_settlements_to_member_id", "to_member_id")]
        [InlineData("ix_settlements_client_request_id", "client_request_id")]
        public async Task Index_OnTheColumn_Exists(string indexName, string column)
        {
            List<string?> indexedColumns = await _database.QueryAsync(
                "SELECT a.attname FROM pg_index i JOIN pg_class c ON c.oid = i.indexrelid JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY (i.indkey) "
                + "WHERE i.indrelid = 'settlements'::regclass AND c.relname = @index_name",
                new NpgsqlParameter("index_name", indexName));

            Assert.Equal(column, Assert.Single(indexedColumns));
        }

        [Fact]
        public async Task ClientRequestIdIndex_IsUnique()
        {
            List<string?> uniqueFlags = await _database.QueryAsync(
                "SELECT i.indisunique::text FROM pg_index i JOIN pg_class c ON c.oid = i.indexrelid WHERE c.relname = 'ix_settlements_client_request_id'");

            Assert.Equal("true", Assert.Single(uniqueFlags));
        }

        [Fact]
        public async Task ValidSettlement_IsAccepted()
        {
            (Guid groupId, Guid fromMemberId, Guid toMemberId, Guid userId) = await NewGroupAsync();

            await SettlementTableRows.InsertSettlementAsync(_database, groupId, fromMemberId, toMemberId, userId);

            Assert.Equal(1, await SettlementTableRows.CountSettlementsAsync(_database, groupId));
        }

        [Fact]
        public async Task ClientRequestId_UsedByTwoSettlements_ViolatesTheUniqueIndex()
        {
            (Guid groupId, Guid fromMemberId, Guid toMemberId, Guid userId) = await NewGroupAsync();
            Guid clientRequestId = Guid.CreateVersion7();
            await SettlementTableRows.InsertSettlementAsync(_database, groupId, fromMemberId, toMemberId, userId, clientRequestId: clientRequestId);

            await RejectedInsert.AssertAsync(
                () => SettlementTableRows.InsertSettlementAsync(_database, groupId, fromMemberId, toMemberId, userId, clientRequestId: clientRequestId),
                PostgresErrorCodes.UniqueViolation);
        }

        [Fact]
        public async Task ClientRequestId_UsedInTwoGroups_ViolatesTheUniqueIndex()
        {
            (Guid firstGroup, Guid firstFrom, Guid firstTo, Guid userId) = await NewGroupAsync();
            (Guid secondGroup, Guid secondFrom, Guid secondTo, _) = await NewGroupAsync();
            Guid clientRequestId = Guid.CreateVersion7();
            await SettlementTableRows.InsertSettlementAsync(_database, firstGroup, firstFrom, firstTo, userId, clientRequestId: clientRequestId);

            await RejectedInsert.AssertAsync(
                () => SettlementTableRows.InsertSettlementAsync(_database, secondGroup, secondFrom, secondTo, userId, clientRequestId: clientRequestId),
                PostgresErrorCodes.UniqueViolation);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task AmountMinor_ZeroOrLess_ViolatesTheCheckConstraint(long amountMinor)
        {
            (Guid groupId, Guid fromMemberId, Guid toMemberId, Guid userId) = await NewGroupAsync();

            await RejectedInsert.AssertAsync(
                () => SettlementTableRows.InsertSettlementAsync(_database, groupId, fromMemberId, toMemberId, userId, amountMinor: amountMinor),
                PostgresErrorCodes.CheckViolation);
        }

        [Fact]
        public async Task AmountMinor_OfOne_IsAccepted()
        {
            (Guid groupId, Guid fromMemberId, Guid toMemberId, Guid userId) = await NewGroupAsync();

            await SettlementTableRows.InsertSettlementAsync(_database, groupId, fromMemberId, toMemberId, userId, amountMinor: 1);

            Assert.Equal(1, await SettlementTableRows.CountSettlementsAsync(_database, groupId));
        }

        [Theory]
        [InlineData("MKD")]
        [InlineData("EUR")]
        public async Task Currency_OfTheTwoKnownNames_IsAccepted(string currency)
        {
            (Guid groupId, Guid fromMemberId, Guid toMemberId, Guid userId) = await NewGroupAsync();

            await SettlementTableRows.InsertSettlementAsync(_database, groupId, fromMemberId, toMemberId, userId, currency: currency);

            Assert.Equal(1, await SettlementTableRows.CountSettlementsAsync(_database, groupId));
        }

        [Theory]
        [InlineData("USD")]
        [InlineData("mkd")]
        [InlineData("eur")]
        [InlineData("")]
        public async Task Currency_OutsideTheTwoKnownNames_ViolatesTheCheckConstraint(string currency)
        {
            (Guid groupId, Guid fromMemberId, Guid toMemberId, Guid userId) = await NewGroupAsync();

            await RejectedInsert.AssertAsync(
                () => SettlementTableRows.InsertSettlementAsync(_database, groupId, fromMemberId, toMemberId, userId, currency: currency),
                PostgresErrorCodes.CheckViolation);
        }

        [Theory]
        [InlineData("Pending")]
        [InlineData("Confirmed")]
        [InlineData("Rejected")]
        [InlineData("Cancelled")]
        public async Task Status_OfTheFourKnownNames_IsAccepted(string status)
        {
            (Guid groupId, Guid fromMemberId, Guid toMemberId, Guid userId) = await NewGroupAsync();

            await SettlementTableRows.InsertSettlementAsync(_database, groupId, fromMemberId, toMemberId, userId, status: status);

            Assert.Equal(1, await SettlementTableRows.CountSettlementsAsync(_database, groupId));
        }

        [Theory]
        [InlineData("pending")]
        [InlineData("Canceled")]
        [InlineData("Deleted")]
        [InlineData("")]
        public async Task Status_OutsideTheFourKnownNames_ViolatesTheCheckConstraint(string status)
        {
            (Guid groupId, Guid fromMemberId, Guid toMemberId, Guid userId) = await NewGroupAsync();

            await RejectedInsert.AssertAsync(
                () => SettlementTableRows.InsertSettlementAsync(_database, groupId, fromMemberId, toMemberId, userId, status: status),
                PostgresErrorCodes.CheckViolation);
        }

        [Fact]
        public async Task FromAndToMember_AreTheSame_ViolatesTheCheckConstraint()
        {
            (Guid groupId, Guid fromMemberId, _, Guid userId) = await NewGroupAsync();

            await RejectedInsert.AssertAsync(
                () => SettlementTableRows.InsertSettlementAsync(_database, groupId, fromMemberId, fromMemberId, userId),
                PostgresErrorCodes.CheckViolation);
        }

        [Fact]
        public async Task Settlement_ForAnUnknownGroup_ViolatesTheForeignKey()
        {
            (_, Guid fromMemberId, Guid toMemberId, Guid userId) = await NewGroupAsync();

            await RejectedInsert.AssertAsync(
                () => SettlementTableRows.InsertSettlementAsync(_database, Guid.CreateVersion7(), fromMemberId, toMemberId, userId),
                PostgresErrorCodes.ForeignKeyViolation);
        }

        [Fact]
        public async Task Settlement_FromAnUnknownMember_ViolatesTheForeignKey()
        {
            (Guid groupId, _, Guid toMemberId, Guid userId) = await NewGroupAsync();

            await RejectedInsert.AssertAsync(
                () => SettlementTableRows.InsertSettlementAsync(_database, groupId, Guid.CreateVersion7(), toMemberId, userId),
                PostgresErrorCodes.ForeignKeyViolation);
        }

        [Fact]
        public async Task Settlement_ToAnUnknownMember_ViolatesTheForeignKey()
        {
            (Guid groupId, Guid fromMemberId, _, Guid userId) = await NewGroupAsync();

            await RejectedInsert.AssertAsync(
                () => SettlementTableRows.InsertSettlementAsync(_database, groupId, fromMemberId, Guid.CreateVersion7(), userId),
                PostgresErrorCodes.ForeignKeyViolation);
        }

        [Fact]
        public async Task MemberWithASettlement_CannotBeDeleted()
        {
            (Guid groupId, Guid fromMemberId, Guid toMemberId, Guid userId) = await NewGroupAsync();
            await SettlementTableRows.InsertSettlementAsync(_database, groupId, fromMemberId, toMemberId, userId);

            await RejectedInsert.AssertAsync(
                () => _database.ExecuteAsync("DELETE FROM group_members WHERE id = @id", new NpgsqlParameter("id", toMemberId)),
                PostgresErrorCodes.RestrictViolation);
        }

        [Fact]
        public async Task Settlement_ByAnUnknownUser_IsAcceptedBecauseTheUserColumnsHaveNoForeignKey()
        {
            (Guid groupId, Guid fromMemberId, Guid toMemberId, _) = await NewGroupAsync();

            await SettlementTableRows.InsertSettlementAsync(_database, groupId, fromMemberId, toMemberId, Guid.CreateVersion7());

            Assert.Equal(1, await SettlementTableRows.CountSettlementsAsync(_database, groupId));
        }

        [Fact]
        public async Task SoftDeletedSettlement_IsAccepted()
        {
            (Guid groupId, Guid fromMemberId, Guid toMemberId, Guid userId) = await NewGroupAsync();

            Guid settlementId = await SettlementTableRows.InsertSettlementAsync(
                _database, groupId, fromMemberId, toMemberId, userId, deletedAt: DateTimeOffset.UtcNow);

            Assert.Equal(userId.ToString(), await SettlementTableRows.SettlementValueAsync(_database, settlementId, "deleted_by_user_id"));
        }

        private async Task<(Guid GroupId, Guid FromMemberId, Guid ToMemberId, Guid UserId)> NewGroupAsync()
        {
            Guid userId = await UserRows.AddAsync(_database);
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_database, userId);
            Guid ownerMemberId = await GroupRows.CurrentMemberIdAsync(_database, groupId, userId);
            Guid otherMemberId = await GroupRows.InsertMemberAsync(_database, groupId, null, "Bojan", userId);

            return (groupId, ownerMemberId, otherMemberId, userId);
        }
    }
}
