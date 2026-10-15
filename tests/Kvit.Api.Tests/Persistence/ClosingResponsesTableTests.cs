using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class ClosingResponsesTableTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        [Theory]
        [InlineData("group_id", "uuid")]
        [InlineData("member_id", "uuid")]
        [InlineData("response", "text")]
        [InlineData("reason", "text")]
        [InlineData("responded_at", "timestamp with time zone")]
        public async Task Column_HasTheExpectedType(string column, string expectedDataType)
        {
            string? dataType = await SchemaQueries.DataTypeAsync(_database, "closing_responses", column);

            Assert.Equal(expectedDataType, dataType);
        }

        [Theory]
        [InlineData("group_id", "NO")]
        [InlineData("member_id", "NO")]
        [InlineData("response", "NO")]
        [InlineData("reason", "YES")]
        [InlineData("responded_at", "NO")]
        public async Task Column_HasTheExpectedNullability(string column, string expectedIsNullable)
        {
            string? isNullable = await SchemaQueries.IsNullableAsync(_database, "closing_responses", column);

            Assert.Equal(expectedIsNullable, isNullable);
        }

        [Fact]
        public async Task Table_HasExactlyTheFiveColumns()
        {
            List<string?> columns = await SchemaQueries.ColumnNamesAsync(_database, "closing_responses");

            Assert.Equal(["group_id", "member_id", "reason", "responded_at", "response"], columns);
        }

        [Fact]
        public async Task PrimaryKey_IsGroupIdTogetherWithMemberId()
        {
            List<string?> primaryKeyColumns = await _database.QueryAsync(
                "SELECT a.attname FROM pg_index i JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY (i.indkey) "
                + "WHERE i.indrelid = 'closing_responses'::regclass AND i.indisprimary ORDER BY a.attname");

            Assert.Equal(["group_id", "member_id"], primaryKeyColumns);
        }

        [Fact]
        public async Task CheckConstraints_AreExactlyTheTwoNamedOnes()
        {
            List<string?> names = await _database.QueryAsync(
                "SELECT conname FROM pg_constraint WHERE conrelid = 'closing_responses'::regclass AND contype = 'c' ORDER BY conname");

            string[] expected =
            [
                "ck_closing_responses_reason_length",
                "ck_closing_responses_response",
            ];
            Assert.Equal(expected, names);
        }

        [Fact]
        public async Task ForeignKeys_AreExactlyTheTwoNamedOnes()
        {
            List<string?> names = await _database.QueryAsync(
                "SELECT conname FROM pg_constraint WHERE conrelid = 'closing_responses'::regclass AND contype = 'f' ORDER BY conname");

            string[] expected =
            [
                "fk_closing_responses_group_members_member_id",
                "fk_closing_responses_groups_group_id",
            ];
            Assert.Equal(expected, names);
        }

        [Fact]
        public async Task ValidResponse_IsAccepted()
        {
            (Guid groupId, Guid memberId) = await NewGroupAsync();

            await ClosingResponseRows.InsertAsync(_database, groupId, memberId);

            Assert.Equal(1, await ClosingResponseRows.CountAsync(_database, groupId));
            Assert.Equal("Confirmed", await ClosingResponseRows.ValueAsync(_database, groupId, memberId, "response"));
            Assert.Null(await ClosingResponseRows.ValueAsync(_database, groupId, memberId, "reason"));
        }

        [Theory]
        [InlineData("Confirmed")]
        [InlineData("Objected")]
        public async Task Response_OfTheTwoKnownNames_IsAccepted(string response)
        {
            (Guid groupId, Guid memberId) = await NewGroupAsync();

            await ClosingResponseRows.InsertAsync(_database, groupId, memberId, response);

            Assert.Equal(response, await ClosingResponseRows.ValueAsync(_database, groupId, memberId, "response"));
        }

        [Theory]
        [InlineData("confirmed")]
        [InlineData("objected")]
        [InlineData("Cancelled")]
        [InlineData("")]
        public async Task Response_OutsideTheTwoKnownNames_ViolatesTheCheckConstraint(string response)
        {
            (Guid groupId, Guid memberId) = await NewGroupAsync();

            await RejectedInsert.AssertAsync(
                () => ClosingResponseRows.InsertAsync(_database, groupId, memberId, response),
                PostgresErrorCodes.CheckViolation);
        }

        [Fact]
        public async Task Reason_OfExactly200Characters_IsAccepted()
        {
            (Guid groupId, Guid memberId) = await NewGroupAsync();

            await ClosingResponseRows.InsertAsync(_database, groupId, memberId, "Objected", new string('x', 200));

            Assert.Equal("200", await ClosingResponseRows.ValueAsync(_database, groupId, memberId, "length(reason)"));
        }

        [Fact]
        public async Task Reason_Of201Characters_ViolatesTheCheckConstraint()
        {
            (Guid groupId, Guid memberId) = await NewGroupAsync();

            await RejectedInsert.AssertAsync(
                () => ClosingResponseRows.InsertAsync(_database, groupId, memberId, "Objected", new string('x', 201)),
                PostgresErrorCodes.CheckViolation);
        }

        [Fact]
        public async Task Reason_Of200CyrillicLetters_IsAcceptedBecauseTheLimitCountsCharactersNotBytes()
        {
            (Guid groupId, Guid memberId) = await NewGroupAsync();

            await ClosingResponseRows.InsertAsync(_database, groupId, memberId, "Objected", new string('а', 200));

            Assert.Equal("200", await ClosingResponseRows.ValueAsync(_database, groupId, memberId, "length(reason)"));
        }

        [Fact]
        public async Task SameMemberAnsweringTwiceInTheSameGroup_ViolatesThePrimaryKey()
        {
            (Guid groupId, Guid memberId) = await NewGroupAsync();
            await ClosingResponseRows.InsertAsync(_database, groupId, memberId);

            await RejectedInsert.AssertAsync(
                () => ClosingResponseRows.InsertAsync(_database, groupId, memberId, "Objected"),
                PostgresErrorCodes.UniqueViolation);
        }

        [Fact]
        public async Task TwoMembersOfTheSameGroup_BothCanAnswer()
        {
            (Guid groupId, Guid firstMemberId) = await NewGroupAsync();
            Guid ownerUserId = Guid.Parse((await GroupRows.GroupValueAsync(_database, groupId, "owner_user_id"))!);
            Guid secondMemberId = await GroupRows.InsertMemberAsync(_database, groupId, null, "Marko", ownerUserId);

            await ClosingResponseRows.InsertAsync(_database, groupId, firstMemberId);
            await ClosingResponseRows.InsertAsync(_database, groupId, secondMemberId);

            Assert.Equal(2, await ClosingResponseRows.CountAsync(_database, groupId));
        }

        [Fact]
        public async Task Response_ForAnUnknownGroup_ViolatesTheForeignKey()
        {
            (_, Guid memberId) = await NewGroupAsync();

            await RejectedInsert.AssertAsync(
                () => ClosingResponseRows.InsertAsync(_database, Guid.CreateVersion7(), memberId),
                PostgresErrorCodes.ForeignKeyViolation);
        }

        [Fact]
        public async Task Response_FromAnUnknownMember_ViolatesTheForeignKey()
        {
            (Guid groupId, _) = await NewGroupAsync();

            await RejectedInsert.AssertAsync(
                () => ClosingResponseRows.InsertAsync(_database, groupId, Guid.CreateVersion7()),
                PostgresErrorCodes.ForeignKeyViolation);
        }

        private async Task<(Guid GroupId, Guid MemberId)> NewGroupAsync()
        {
            Guid userId = await UserRows.AddAsync(_database);
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(_database, userId);
            Guid memberId = await GroupRows.CurrentMemberIdAsync(_database, groupId, userId);

            return (groupId, memberId);
        }
    }
}
