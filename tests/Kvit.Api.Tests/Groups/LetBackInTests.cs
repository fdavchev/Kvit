using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class LetBackInTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Fact]
        public async Task LetBackIn_RemovedAccountMember_Answers204AndTheSameRowIsCurrentAgain()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, "Removed");
            int rowsBefore = await GroupRows.CountMembersAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.LetBackInAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, setup.MemberRowId, "removed_at"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, setup.MemberRowId, "removed_by_user_id"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, setup.MemberRowId, "end_kind"));
            Assert.Equal(setup.Member.UserId.ToString(), await GroupRows.MemberRowValueAsync(_app, setup.MemberRowId, "user_id"));
            Assert.Equal(rowsBefore, await GroupRows.CountMembersAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task LetBackIn_RemovedPlainName_ClearsTheRemovedFieldsOnTheSameRow()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId, DateTimeOffset.UtcNow, "Removed");

            HttpResponseMessage response = await GroupRequests.LetBackInAsync(setup.Owner.Client, setup.GroupId, plainId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, plainId, "removed_at"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, plainId, "removed_by_user_id"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, plainId, "end_kind"));
            Assert.Contains(plainId, await GroupRequests.MemberIdsAsync(setup.Owner.Client, setup.GroupId));
        }

        [Fact]
        public async Task LetBackIn_RemovedAccountMember_SeesTheGroupAgain()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRequests.RemoveMemberAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            await GroupRequests.LetBackInAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            HttpResponseMessage response = await GroupRequests.GetAsync(setup.Member.Client, setup.GroupId);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement removed = (await GroupRequests.ReadMembersAsync(setup.Owner.Client, setup.GroupId)).GetProperty("removed");
            Assert.Empty(removed.EnumerateArray());
        }

        [Fact]
        public async Task LetBackIn_Owner_WritesOneMemberLetBackInEventByTheOwnerAboutTheRow()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, "Removed");

            await GroupRequests.LetBackInAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, setup.GroupId, "MemberLetBackIn"));
            Assert.Equal(setup.Owner.UserId.ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "MemberLetBackIn", "actor_user_id"));
            Assert.Equal(setup.MemberRowId.ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "MemberLetBackIn", "member_id"));
        }

        [Theory]
        [InlineData("Left")]
        [InlineData("SetAside")]
        public async Task LetBackIn_RowThatLeftOrWasSetAside_Answers404MemberNotFoundAndWritesNothing(string endKind)
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, endKind);
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.LetBackInAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task LetBackIn_CurrentRow_Answers404MemberNotFoundAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.LetBackInAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task LetBackIn_UnknownMemberId_Answers404MemberNotFound()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            HttpResponseMessage response = await GroupRequests.LetBackInAsync(setup.Owner.Client, setup.GroupId, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public async Task LetBackIn_RemovedRowOfAnotherGroup_Answers404MemberNotFoundAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid otherGroupId = await _app.CreateGroupAsync(setup.Owner);
            Guid otherRemovedId = await GroupRows.InsertMemberAsync(_app, otherGroupId, null, "Marko", setup.Owner.UserId, DateTimeOffset.UtcNow, "Removed");

            HttpResponseMessage response = await GroupRequests.LetBackInAsync(setup.Owner.Client, setup.GroupId, otherRemovedId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal("Removed", await GroupRows.MemberRowValueAsync(_app, otherRemovedId, "end_kind"));
        }

        [Fact]
        public async Task LetBackIn_PlainNameWhoseNameIsNowTakenByAPlainName_Answers400NameTakenAndStaysRemoved()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid removedId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId, DateTimeOffset.UtcNow, "Removed");
            await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "marko", setup.Owner.UserId);
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.LetBackInAsync(setup.Owner.Client, setup.GroupId, removedId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_TAKEN);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task LetBackIn_PlainNameWhoseNameIsNowTakenByAnAccountMember_Answers400NameTaken()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid removedId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, GroupsApp.MemberName, setup.Owner.UserId, DateTimeOffset.UtcNow, "Removed");

            HttpResponseMessage response = await GroupRequests.LetBackInAsync(setup.Owner.Client, setup.GroupId, removedId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NAME_TAKEN);
        }

        [Fact]
        public async Task LetBackIn_MemberWhoIsNotTheOwner_Answers403GroupNotOwnerAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid removedId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId, DateTimeOffset.UtcNow, "Removed");
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.LetBackInAsync(setup.Member.Client, setup.GroupId, removedId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task LetBackIn_TwiceInARow_Answers404MemberNotFoundTheSecondTimeAndWritesOneEvent()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, "Removed");
            await GroupRequests.LetBackInAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            HttpResponseMessage second = await GroupRequests.LetBackInAsync(setup.Owner.Client, setup.GroupId, setup.MemberRowId);

            await ProblemResponse.AssertAsync(second, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, setup.GroupId, "MemberLetBackIn"));
        }
    }
}
