using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class ResetInviteLinkTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Fact]
        public async Task Reset_Owner_Answers200WithExactlyAnInviteTokenOf43Base64UrlCharacters()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            HttpResponseMessage response = await GroupRequests.ResetInviteAsync(setup.Owner.Client, setup.GroupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(["inviteToken"], GroupRequests.PropertyNamesOf(body));
            string newToken = body.GetProperty("inviteToken").GetString()!;
            Assert.Matches(GroupRequests.TokenFormat, newToken);
            Assert.NotEqual(setup.InviteToken, newToken);
        }

        [Fact]
        public async Task Reset_Owner_StoresTheNewTokenKeepsTheOldOneAsPreviousAndRefreshesTheCreatedAt()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRows.AgeInviteTokenAsync(_app, setup.GroupId);

            JsonElement body = await AuthRequests.ReadJsonAsync(await GroupRequests.ResetInviteAsync(setup.Owner.Client, setup.GroupId));

            Assert.Equal(body.GetProperty("inviteToken").GetString(), await GroupRows.GroupValueAsync(_app, setup.GroupId, "invite_token"));
            Assert.Equal(setup.InviteToken, await GroupRows.GroupValueAsync(_app, setup.GroupId, "previous_invite_token"));
            Assert.Equal("true", await GroupRows.GroupValueAsync(_app, setup.GroupId, "invite_token_created_at > now() - interval '1 minute'"));
        }

        [Fact]
        public async Task Reset_Owner_WritesOneInviteLinkResetEventByTheOwner()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();

            await GroupRequests.ResetInviteAsync(setup.Owner.Client, setup.GroupId);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, setup.GroupId, "InviteLinkReset"));
            Assert.Equal(setup.Owner.UserId.ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "InviteLinkReset", "actor_user_id"));
        }

        [Fact]
        public async Task Reset_Owner_ShowsTheNewTokenInGetGroupForAnyMember()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            JsonElement body = await AuthRequests.ReadJsonAsync(await GroupRequests.ResetInviteAsync(setup.Owner.Client, setup.GroupId));

            JsonElement group = await AuthRequests.ReadJsonAsync(await GroupRequests.GetAsync(setup.Member.Client, setup.GroupId));

            Assert.Equal(body.GetProperty("inviteToken").GetString(), group.GetProperty("inviteToken").GetString());
        }

        [Fact]
        public async Task Reset_Owner_MakesThePreviewOfTheOldTokenAnswer404InviteNotFoundAndTheNewTokenOpen()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            JsonElement body = await AuthRequests.ReadJsonAsync(await GroupRequests.ResetInviteAsync(setup.Owner.Client, setup.GroupId));
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage oldTokenResponse = await GroupRequests.PreviewAsync(anonymous, setup.InviteToken);
            HttpResponseMessage newTokenResponse = await GroupRequests.PreviewAsync(anonymous, body.GetProperty("inviteToken").GetString()!);

            await ProblemResponse.AssertAsync(oldTokenResponse, HttpStatusCode.NotFound, ResultCodes.INVITE_NOT_FOUND);
            Assert.Equal(HttpStatusCode.OK, newTokenResponse.StatusCode);
        }

        [Fact]
        public async Task Reset_Twice_KeepsOnlyTheTokenBeforeTheLastResetAsPrevious()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            JsonElement first = await AuthRequests.ReadJsonAsync(await GroupRequests.ResetInviteAsync(setup.Owner.Client, setup.GroupId));

            JsonElement second = await AuthRequests.ReadJsonAsync(await GroupRequests.ResetInviteAsync(setup.Owner.Client, setup.GroupId));

            string firstToken = first.GetProperty("inviteToken").GetString()!;
            Assert.Equal(firstToken, await GroupRows.GroupValueAsync(_app, setup.GroupId, "previous_invite_token"));
            Assert.Equal(second.GetProperty("inviteToken").GetString(), await GroupRows.GroupValueAsync(_app, setup.GroupId, "invite_token"));
            Assert.NotEqual(firstToken, second.GetProperty("inviteToken").GetString());
            HttpResponseMessage originalResponse = await GroupRequests.PreviewAsync(_app.CreateClient(), setup.InviteToken);
            await ProblemResponse.AssertAsync(originalResponse, HttpStatusCode.NotFound, ResultCodes.INVITE_NOT_FOUND);
        }

        [Fact]
        public async Task Reset_MemberWhoIsNotTheOwner_Answers403GroupNotOwnerAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.ResetInviteAsync(setup.Member.Client, setup.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task UndoReset_AfterAReset_Answers200WithThePreviousTokenAndRestoresIt()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            JsonElement reset = await AuthRequests.ReadJsonAsync(await GroupRequests.ResetInviteAsync(setup.Owner.Client, setup.GroupId));
            await GroupRows.AgeInviteTokenAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.UndoResetInviteAsync(setup.Owner.Client, setup.GroupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(["inviteToken"], GroupRequests.PropertyNamesOf(body));
            Assert.Equal(setup.InviteToken, body.GetProperty("inviteToken").GetString());
            Assert.Equal(setup.InviteToken, await GroupRows.GroupValueAsync(_app, setup.GroupId, "invite_token"));
            Assert.Null(await GroupRows.GroupValueAsync(_app, setup.GroupId, "previous_invite_token"));
            Assert.Equal("true", await GroupRows.GroupValueAsync(_app, setup.GroupId, "invite_token_created_at > now() - interval '1 minute'"));
            Assert.NotEqual(reset.GetProperty("inviteToken").GetString(), body.GetProperty("inviteToken").GetString());
        }

        [Fact]
        public async Task UndoReset_AfterAReset_WritesOneInviteLinkRestoredEventByTheOwner()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRequests.ResetInviteAsync(setup.Owner.Client, setup.GroupId);

            await GroupRequests.UndoResetInviteAsync(setup.Owner.Client, setup.GroupId);

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, setup.GroupId, "InviteLinkRestored"));
            Assert.Equal(setup.Owner.UserId.ToString(), await GroupRows.EventValueAsync(_app, setup.GroupId, "InviteLinkRestored", "actor_user_id"));
        }

        [Fact]
        public async Task UndoReset_AfterAReset_MakesTheRestoredTokenOpenAgainAndTheResetTokenNotFound()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            JsonElement reset = await AuthRequests.ReadJsonAsync(await GroupRequests.ResetInviteAsync(setup.Owner.Client, setup.GroupId));
            await GroupRequests.UndoResetInviteAsync(setup.Owner.Client, setup.GroupId);
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage restoredResponse = await GroupRequests.PreviewAsync(anonymous, setup.InviteToken);
            HttpResponseMessage resetTokenResponse = await GroupRequests.PreviewAsync(anonymous, reset.GetProperty("inviteToken").GetString()!);

            Assert.Equal(HttpStatusCode.OK, restoredResponse.StatusCode);
            await ProblemResponse.AssertAsync(resetTokenResponse, HttpStatusCode.NotFound, ResultCodes.INVITE_NOT_FOUND);
        }

        [Fact]
        public async Task UndoReset_WithNothingToUndo_Answers400NothingToUndoAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.UndoResetInviteAsync(setup.Owner.Client, setup.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.INVITE_NOTHING_TO_UNDO);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }

        [Fact]
        public async Task UndoReset_TwiceInARow_Answers400NothingToUndoTheSecondTime()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRequests.ResetInviteAsync(setup.Owner.Client, setup.GroupId);
            await GroupRequests.UndoResetInviteAsync(setup.Owner.Client, setup.GroupId);

            HttpResponseMessage second = await GroupRequests.UndoResetInviteAsync(setup.Owner.Client, setup.GroupId);

            await ProblemResponse.AssertAsync(second, HttpStatusCode.BadRequest, ResultCodes.INVITE_NOTHING_TO_UNDO);
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, setup.GroupId, "InviteLinkRestored"));
        }

        [Fact]
        public async Task Reset_AfterAnUndo_Answers200WithANewTokenAndKeepsTheRestoredOneAsPrevious()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            JsonElement firstReset = await AuthRequests.ReadJsonAsync(await GroupRequests.ResetInviteAsync(setup.Owner.Client, setup.GroupId));
            await GroupRequests.UndoResetInviteAsync(setup.Owner.Client, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.ResetInviteAsync(setup.Owner.Client, setup.GroupId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string newToken = (await AuthRequests.ReadJsonAsync(response)).GetProperty("inviteToken").GetString()!;
            Assert.NotEqual(setup.InviteToken, newToken);
            Assert.NotEqual(firstReset.GetProperty("inviteToken").GetString(), newToken);
            Assert.Equal(newToken, await GroupRows.GroupValueAsync(_app, setup.GroupId, "invite_token"));
            Assert.Equal(setup.InviteToken, await GroupRows.GroupValueAsync(_app, setup.GroupId, "previous_invite_token"));
        }

        [Fact]
        public async Task UndoReset_MemberWhoIsNotTheOwner_Answers403GroupNotOwnerAndWritesNothing()
        {
            GroupSetup setup = await _app.CreateGroupWithMemberAsync();
            await GroupRequests.ResetInviteAsync(setup.Owner.Client, setup.GroupId);
            string before = await GroupRows.SnapshotAsync(_app, setup.GroupId);

            HttpResponseMessage response = await GroupRequests.UndoResetInviteAsync(setup.Member.Client, setup.GroupId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, setup.GroupId));
        }
    }
}
