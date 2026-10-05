using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class MemberRoutingTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Theory]
        [MemberData(nameof(MemberEndpoints.All), MemberType = typeof(MemberEndpoints))]
        public async Task GroupId_ThatIsNotAGuid_Answers404WhileAnUnknownGuidIsRoutedToTheGroupLookup(string method, string suffix)
        {
            SignedInUser user = await _app.SignUpAsync();
            string guid = Guid.CreateVersion7().ToString();
            object body = new { name = "Zed", memberId = guid };

            HttpResponseMessage routed = await GroupRequests.SendAsync(user.Client, method, MemberEndpoints.PathFor(guid, suffix, guid), body);
            HttpResponseMessage notAGuid = await GroupRequests.SendAsync(user.Client, method, MemberEndpoints.PathFor("not-a-guid", suffix, guid), body);

            await ProblemResponse.AssertAsync(routed, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(HttpStatusCode.NotFound, notAGuid.StatusCode);
        }

        [Theory]
        [MemberData(nameof(MemberEndpoints.WithMemberId), MemberType = typeof(MemberEndpoints))]
        public async Task MemberId_ThatIsNotAGuid_Answers404WhileAnUnknownGuidIsRoutedToTheGroupLookup(string method, string suffix)
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(user);
            string guid = Guid.CreateVersion7().ToString();
            object body = new { name = "Zed", memberId = guid };

            HttpResponseMessage routed = await GroupRequests.SendAsync(user.Client, method, MemberEndpoints.PathFor(Guid.CreateVersion7().ToString(), suffix, guid), body);
            HttpResponseMessage notAGuid = await GroupRequests.SendAsync(user.Client, method, MemberEndpoints.PathFor(groupId.ToString(), suffix, "not-a-guid"), body);

            await ProblemResponse.AssertAsync(routed, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(HttpStatusCode.NotFound, notAGuid.StatusCode);
        }
    }
}
