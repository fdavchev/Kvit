using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Postgres;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class GroupsApp(PostgresFixture postgres) : AuthApp(postgres)
    {
        public const string OwnerName = "Ana";
        public const string MemberName = "Bojan";

        public async Task<SignedInUser> SignUpAsync(string displayName = OwnerName)
        {
            RegistrationForm form = RegistrationForm.Valid() with { DisplayName = displayName };
            HttpClient client = CreateClient();

            HttpResponseMessage response = await AuthRequests.RegisterAsync(client, form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Guid userId = (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid();
            return new SignedInUser(client, userId, form);
        }

        public async Task<Guid> CreateGroupAsync(SignedInUser owner)
        {
            HttpResponseMessage response = await GroupRequests.CreateAsync(owner.Client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid();
        }

        public async Task<GroupSetup> CreateGroupWithMemberAsync()
        {
            SignedInUser owner = await SignUpAsync(OwnerName);
            SignedInUser member = await SignUpAsync(MemberName);
            Guid groupId = await CreateGroupAsync(owner);
            Guid ownerRowId = await GroupRows.CurrentMemberIdAsync(this, groupId, owner.UserId);
            Guid memberRowId = await GroupRows.InsertMemberAsync(this, groupId, member.UserId, member.Form.DisplayName, member.UserId);
            string inviteToken = (await GroupRows.GroupValueAsync(this, groupId, "invite_token"))!;

            return new GroupSetup(groupId, inviteToken, owner, ownerRowId, member, memberRowId);
        }

        public async Task<Guid> SetUpClaimedNameAsync(GroupSetup setup, string name)
        {
            await GroupRows.EndMembershipAsync(this, setup.GroupId, setup.Member.UserId, "SetAside");

            return await GroupRows.InsertMemberAsync(this, setup.GroupId, setup.Member.UserId, name, setup.Member.UserId, claimedAt: DateTimeOffset.UtcNow);
        }
    }
}
