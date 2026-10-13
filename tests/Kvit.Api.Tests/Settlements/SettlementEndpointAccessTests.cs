using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public class SettlementEndpointAccessTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Theory]
        [MemberData(nameof(SettlementEndpoints.All), MemberType = typeof(SettlementEndpoints))]
        public async Task Anonymous_Answers401WhileTheSameRouteExistsForASignedInUser(string method, string suffix)
        {
            SignedInUser user = await _app.SignUpAsync();
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage routed = await SettlementEndpoints.SendAsync(user.Client, method, suffix, Guid.CreateVersion7(), Guid.CreateVersion7());
            HttpResponseMessage response = await SettlementEndpoints.SendAsync(anonymous, method, suffix, Guid.CreateVersion7(), Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(routed, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(SettlementEndpoints.All), MemberType = typeof(SettlementEndpoints))]
        public async Task Anonymous_ForAnExistingGroup_Answers401AndWritesNothing(string method, string suffix)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.ConfirmedBojanToAnaAsync(group);
            HttpClient anonymous = _app.CreateClient();
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementEndpoints.SendAsync(anonymous, method, suffix, group.GroupId, settlementId);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Theory]
        [MemberData(nameof(SettlementEndpoints.All), MemberType = typeof(SettlementEndpoints))]
        public async Task NonMember_Answers404GroupNotFoundAndWritesNothing(string method, string suffix)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            SignedInUser stranger = await _app.SignUpAsync("Cvetan");
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementEndpoints.SendAsync(stranger.Client, method, suffix, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Theory]
        [MemberData(nameof(SettlementEndpoints.All), MemberType = typeof(SettlementEndpoints))]
        public async Task RemovedMember_Answers404GroupNotFoundAndWritesNothing(string method, string suffix)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, "Removed");
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementEndpoints.SendAsync(group.Member.Client, method, suffix, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Theory]
        [MemberData(nameof(SettlementEndpoints.All), MemberType = typeof(SettlementEndpoints))]
        public async Task MemberWhoLeft_Answers404GroupNotFoundAndWritesNothing(string method, string suffix)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            await GroupRows.EndMembershipAsync(_app, group.GroupId, group.Member.UserId, "Left");
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementEndpoints.SendAsync(group.Member.Client, method, suffix, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Theory]
        [MemberData(nameof(SettlementEndpoints.All), MemberType = typeof(SettlementEndpoints))]
        public async Task DeletedGroup_Answers404GroupNotFoundEvenForTheOwnerAndWritesNothing(string method, string suffix)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            await GroupRows.SetDeletedAsync(_app, group.GroupId, group.Owner.UserId, 0);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await SettlementEndpoints.SendAsync(group.Owner.Client, method, suffix, group.GroupId, settlementId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Theory]
        [MemberData(nameof(SettlementEndpoints.All), MemberType = typeof(SettlementEndpoints))]
        public async Task GroupId_ThatIsNotAGuid_Answers404(string method, string suffix)
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage routed = await SettlementEndpoints.SendAsync(user.Client, method, suffix, Guid.CreateVersion7(), Guid.CreateVersion7());
            HttpResponseMessage response = await SettlementEndpoints.SendToSegmentsAsync(user.Client, method, suffix, "not-a-guid", Guid.CreateVersion7().ToString());

            await ProblemResponse.AssertAsync(routed, HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(SettlementEndpoints.WithSettlementId), MemberType = typeof(SettlementEndpoints))]
        public async Task SettlementId_ThatIsNotAGuid_Answers404(string method, string suffix)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            HttpResponseMessage routed = await SettlementEndpoints.SendAsync(group.Owner.Client, method, suffix, group.GroupId, Guid.CreateVersion7());
            HttpResponseMessage response = await SettlementEndpoints.SendToSegmentsAsync(group.Owner.Client, method, suffix, group.GroupId.ToString(), "not-a-guid");

            await ProblemResponse.AssertAsync(routed, HttpStatusCode.NotFound, ResultCodes.SETTLEMENT_NOT_FOUND);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task EveryCurrentMember_CanReadTheList()
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);

            HttpResponseMessage forOwner = await SettlementRequests.ListAsync(group.Owner.Client, group.GroupId);
            HttpResponseMessage forMember = await SettlementRequests.ListAsync(group.Member.Client, group.GroupId);
            HttpResponseMessage forThird = await SettlementRequests.ListAsync(group.Third.Client, group.GroupId);

            Assert.Equal(HttpStatusCode.OK, forOwner.StatusCode);
            Assert.Equal(HttpStatusCode.OK, forMember.StatusCode);
            Assert.Equal(HttpStatusCode.OK, forThird.StatusCode);
        }

        [Theory]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        public async Task ListRoute_OtherMethods_Answer405AndChangeNothing(string method)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            await SettlementSteps.ConfirmedBojanToAnaAsync(group);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.SendAsync(group.Owner.Client, method, SettlementRequests.SettlementsPathOf(group.GroupId), new { });

            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("PUT")]
        public async Task ActionRoutes_ThatChangeThings_RefuseTheMethodsThatDoNotBelongToThem(string method)
        {
            SettlementGroup group = await SettlementSteps.CreateGroupAsync(_app);
            Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);
            string before = await SettlementRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage confirm = await GroupRequests.SendAsync(group.Owner.Client, method, $"{SettlementRequests.SettlementPathOf(group.GroupId, settlementId)}/confirm", new { });
            HttpResponseMessage reject = await GroupRequests.SendAsync(group.Owner.Client, method, $"{SettlementRequests.SettlementPathOf(group.GroupId, settlementId)}/reject", new { });
            HttpResponseMessage cancel = await GroupRequests.SendAsync(group.Member.Client, method, $"{SettlementRequests.SettlementPathOf(group.GroupId, settlementId)}/cancel", new { });

            Assert.Equal(HttpStatusCode.MethodNotAllowed, confirm.StatusCode);
            Assert.Equal(HttpStatusCode.MethodNotAllowed, reject.StatusCode);
            Assert.Equal(HttpStatusCode.MethodNotAllowed, cancel.StatusCode);
            Assert.Equal(before, await SettlementRows.SnapshotAsync(_app, group.GroupId));
        }
    }
}
