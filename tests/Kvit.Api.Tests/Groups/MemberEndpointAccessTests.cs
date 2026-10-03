using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class MemberEndpointAccessTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Theory]
        [MemberData(nameof(MemberEndpoints.All), MemberType = typeof(MemberEndpoints))]
        public async Task Anonymous_Answers401WhileTheSameRouteExistsForASignedInUser(string method, string suffix)
        {
            SignedInUser user = await _app.SignUpAsync();
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage routed = await MemberEndpoints.SendAsync(user.Client, method, suffix, Guid.CreateVersion7(), Guid.CreateVersion7());
            HttpResponseMessage response = await MemberEndpoints.SendAsync(anonymous, method, suffix, Guid.CreateVersion7(), Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(routed, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(MemberEndpoints.All), MemberType = typeof(MemberEndpoints))]
        public async Task NonMember_Answers404GroupNotFoundAndWritesNothing(string method, string suffix)
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await MemberEndpoints.SendAsync(stranger.Client, method, suffix, setup.GroupId, setup.MemberRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Theory]
        [MemberData(nameof(MemberEndpoints.All), MemberType = typeof(MemberEndpoints))]
        public async Task RemovedMember_Answers404GroupNotFoundAndWritesNothing(string method, string suffix)
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.EndMembershipAsync(_app, setup.GroupId, setup.Member.UserId, "Removed");
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await MemberEndpoints.SendAsync(setup.Member.Client, method, suffix, setup.GroupId, setup.MemberRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Theory]
        [MemberData(nameof(MemberEndpoints.All), MemberType = typeof(MemberEndpoints))]
        public async Task DeletedGroup_Answers404GroupNotFoundEvenForTheOwnerAndWritesNothing(string method, string suffix)
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            Guid plainId = await GroupRows.InsertMemberAsync(_app, setup.GroupId, null, "Marko", setup.Owner.UserId);
            await GroupRows.SetDeletedAsync(_app, setup.GroupId, setup.Owner.UserId, 0);
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await MemberEndpoints.SendAsync(setup.Owner.Client, method, suffix, setup.GroupId, plainId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Theory]
        [MemberData(nameof(MemberEndpoints.OwnerOnly), MemberType = typeof(MemberEndpoints))]
        public async Task OwnerOnlyEndpoint_MemberWhoIsNotTheOwner_Answers403BeforeLookingAtTheMemberAndWritesNothing(string method, string suffix)
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await MemberEndpoints.SendAsync(setup.Member.Client, method, suffix, setup.GroupId, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }
    }
}
