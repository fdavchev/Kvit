using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class GroupMembersTableTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        [Theory]
        [InlineData("id", "uuid")]
        [InlineData("group_id", "uuid")]
        [InlineData("user_id", "uuid")]
        [InlineData("name", "character varying")]
        [InlineData("joined_at", "timestamp with time zone")]
        [InlineData("added_by_user_id", "uuid")]
        [InlineData("claimed_at", "timestamp with time zone")]
        [InlineData("removed_at", "timestamp with time zone")]
        [InlineData("removed_by_user_id", "uuid")]
        [InlineData("end_kind", "text")]
        public async Task Column_HasTheExpectedType(string column, string expectedDataType)
        {
            string? dataType = await SchemaQueries.DataTypeAsync(_database, "group_members", column);

            Assert.Equal(expectedDataType, dataType);
        }

        [Theory]
        [InlineData("id", "NO")]
        [InlineData("group_id", "NO")]
        [InlineData("user_id", "YES")]
        [InlineData("name", "NO")]
        [InlineData("joined_at", "NO")]
        [InlineData("added_by_user_id", "NO")]
        [InlineData("claimed_at", "YES")]
        [InlineData("removed_at", "YES")]
        [InlineData("removed_by_user_id", "YES")]
        [InlineData("end_kind", "YES")]
        public async Task Column_HasTheExpectedNullability(string column, string expectedIsNullable)
        {
            string? isNullable = await SchemaQueries.IsNullableAsync(_database, "group_members", column);

            Assert.Equal(expectedIsNullable, isNullable);
        }

        [Fact]
        public async Task Name_IsLimitedTo60Characters()
        {
            string? maxLength = await SchemaQueries.MaxLengthAsync(_database, "group_members", "name");

            Assert.Equal("60", maxLength);
        }

        [Fact]
        public async Task Name_Empty_ViolatesTheCheckConstraint()
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(
                () => GroupRows.InsertMemberAsync(_database, groupId, userId, "", userId),
                PostgresErrorCodes.CheckViolation);
        }

        [Theory]
        [InlineData("Left")]
        [InlineData("Removed")]
        [InlineData("SetAside")]
        public async Task EndKind_OfTheThreeKnownNames_IsAccepted(string endKind)
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);
            Guid userId = await UserRows.AddAsync(_database);

            await GroupRows.InsertMemberAsync(_database, groupId, userId, "Ana", userId, DateTimeOffset.UtcNow, endKind);

            Assert.Equal(endKind, await GroupRows.MemberValueAsync(_database, groupId, userId, "end_kind"));
        }

        [Theory]
        [InlineData("Bogus")]
        [InlineData("left")]
        [InlineData("")]
        public async Task EndKind_OutsideTheThreeKnownNames_ViolatesTheCheckConstraint(string endKind)
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(
                () => GroupRows.InsertMemberAsync(_database, groupId, userId, "Ana", userId, DateTimeOffset.UtcNow, endKind),
                PostgresErrorCodes.CheckViolation);
        }

        [Fact]
        public async Task RemovedAt_WithoutAnEndKind_ViolatesTheCheckConstraint()
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(
                () => GroupRows.InsertMemberAsync(_database, groupId, userId, "Ana", userId, DateTimeOffset.UtcNow, null),
                PostgresErrorCodes.CheckViolation);
        }

        [Fact]
        public async Task EndKind_WithoutARemovedAt_ViolatesTheCheckConstraint()
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(
                () => GroupRows.InsertMemberAsync(_database, groupId, userId, "Ana", userId, null, "Removed"),
                PostgresErrorCodes.CheckViolation);
        }

        [Fact]
        public async Task ActiveMember_WithoutRemovedAtAndEndKind_IsAccepted()
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);
            Guid userId = await UserRows.AddAsync(_database);

            await GroupRows.InsertMemberAsync(_database, groupId, userId, "Ana", userId);

            Assert.Equal(1, await GroupRows.CountMembersAsync(_database, groupId));
        }

        [Fact]
        public async Task ActiveMembership_OfTheSameUserTwiceInOneGroup_ViolatesTheUniqueIndex()
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);
            Guid userId = await UserRows.AddAsync(_database);
            await GroupRows.InsertMemberAsync(_database, groupId, userId, "Ana", userId);

            await RejectedInsert.AssertAsync(
                () => GroupRows.InsertMemberAsync(_database, groupId, userId, "Ana", userId),
                PostgresErrorCodes.UniqueViolation);
        }

        [Fact]
        public async Task ActiveMembership_AfterTheFirstOneEnded_IsAllowed()
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);
            Guid userId = await UserRows.AddAsync(_database);
            await GroupRows.InsertMemberAsync(_database, groupId, userId, "Ana", userId, DateTimeOffset.UtcNow, "Left");

            await GroupRows.InsertMemberAsync(_database, groupId, userId, "Ana", userId);

            Assert.Equal(2, await GroupRows.CountMembersAsync(_database, groupId));
        }

        [Fact]
        public async Task EndedMemberships_OfTheSameUserTwiceInOneGroup_AreAllowed()
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);
            Guid userId = await UserRows.AddAsync(_database);
            await GroupRows.InsertMemberAsync(_database, groupId, userId, "Ana", userId, DateTimeOffset.UtcNow, "Left");

            await GroupRows.InsertMemberAsync(_database, groupId, userId, "Ana", userId, DateTimeOffset.UtcNow, "Removed");

            Assert.Equal(2, await GroupRows.CountMembersAsync(_database, groupId));
        }

        [Fact]
        public async Task ActiveMembership_OfTheSameUserInTwoGroups_IsAllowed()
        {
            Guid firstGroupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);
            Guid secondGroupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);
            Guid userId = await UserRows.AddAsync(_database);
            await GroupRows.InsertMemberAsync(_database, firstGroupId, userId, "Ana", userId);

            await GroupRows.InsertMemberAsync(_database, secondGroupId, userId, "Ana", userId);

            Assert.Equal(1, await GroupRows.CountMembersAsync(_database, secondGroupId));
        }

        [Fact]
        public async Task PlainNames_RepeatedInOneGroup_AreAllowedByTheDatabase()
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);
            Guid addedByUserId = await UserRows.AddAsync(_database);
            await GroupRows.InsertMemberAsync(_database, groupId, null, "Marko", addedByUserId);

            await GroupRows.InsertMemberAsync(_database, groupId, null, "Marko", addedByUserId);

            Assert.Equal(2, await GroupRows.CountMembersAsync(_database, groupId));
        }

        [Fact]
        public async Task Member_ForAnUnknownGroup_ViolatesTheForeignKey()
        {
            Guid userId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(
                () => GroupRows.InsertMemberAsync(_database, Guid.CreateVersion7(), userId, "Ana", userId),
                PostgresErrorCodes.ForeignKeyViolation);
        }

        [Fact]
        public async Task Member_ForAnUnknownUser_ViolatesTheForeignKey()
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);
            Guid addedByUserId = await UserRows.AddAsync(_database);

            await RejectedInsert.AssertAsync(
                () => GroupRows.InsertMemberAsync(_database, groupId, Guid.CreateVersion7(), "Ana", addedByUserId),
                PostgresErrorCodes.ForeignKeyViolation);
        }
    }
}
