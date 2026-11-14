using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.OwnerTools
{
    public class OwnerToolsReasonTests(OwnerToolsApp _app) : IClassFixture<OwnerToolsApp>
    {
        private static readonly string FiveHundred = new('r', OwnerToolsRequests.ReasonMaxLength);

        public static TheoryData<string, string> NamesWithEveryValidReason
        {
            get
            {
                TheoryData<string, string> data = [];
                foreach (string name in new[] { OwnerToolsCalls.Suspend, OwnerToolsCalls.Unsuspend, OwnerToolsCalls.EraseName, OwnerToolsCalls.Close, OwnerToolsCalls.Reopen })
                {
                    foreach (string reasonCase in new[] { "OneCharacter", "Exactly500", "Exactly500WithSpacesAround", "InnerWhitespaceKept", "Macedonian" })
                    {
                        data.Add(name, reasonCase);
                    }
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(OwnerToolsCalls.NamesWithEveryInvalidReason), MemberType = typeof(OwnerToolsCalls))]
        public async Task Reason_Invalid_Answers400ReasonInvalidAndChangesNothing(string name, string json)
        {
            OwnerToolsCall call = OwnerToolsCalls.Named(name);
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser target = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(await _app.SignUpAsync());
            await OwnerToolsSteps.PrepareAsync(_app, call, target.UserId, groupId);
            string before = await OwnerToolsSteps.SnapshotAsync(_app, target.UserId, groupId);
            int logRowsBefore = await OwnerToolsRows.CountActionsAsync(_app);

            HttpResponseMessage response = await GroupRequests.PostJsonTextAsync(admin.Client, call.PathOf(target.UserId, groupId), json);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, OwnerToolsRequests.ReasonInvalidCode);
            Assert.Equal(before, await OwnerToolsSteps.SnapshotAsync(_app, target.UserId, groupId));
            Assert.Equal(logRowsBefore, await OwnerToolsRows.CountActionsAsync(_app));
        }

        [Theory]
        [MemberData(nameof(NamesWithEveryValidReason))]
        public async Task Reason_Valid_Answers204AndLogsItTrimmed(string name, string reasonCase)
        {
            OwnerToolsCall call = OwnerToolsCalls.Named(name);
            (string sent, string stored) = ReasonOf(reasonCase);
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser target = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(await _app.SignUpAsync());
            await OwnerToolsSteps.PrepareAsync(_app, call, target.UserId, groupId);

            HttpResponseMessage response = await call.SendAsync(admin.Client, target.UserId, groupId, sent);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Guid targetId = call.Target == OwnerToolsTarget.Person ? target.UserId : groupId;
            StoredAction row = Assert.Single(await OwnerToolsRows.ActionsAboutAsync(_app, targetId));
            Assert.Equal(stored, row.Reason);
        }

        [Fact]
        public async Task Reason_InvalidAfterAValidOne_DoesNotUndoTheValidOne()
        {
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser target = await _app.SignUpAsync();
            await OwnerToolsSteps.SuspendAsync(admin, target.UserId);

            HttpResponseMessage refused = await OwnerToolsRequests.UnsuspendAsync(admin.Client, target.UserId, "   ");

            await ProblemResponse.AssertAsync(refused, HttpStatusCode.BadRequest, OwnerToolsRequests.ReasonInvalidCode);
            Assert.NotNull(await OwnerToolsRows.SuspendedAtAsync(_app, target.UserId));
            Assert.Single(await OwnerToolsRows.ActionsAboutAsync(_app, target.UserId));
        }

        private static (string Sent, string Stored) ReasonOf(string reasonCase)
        {
            return reasonCase switch
            {
                "OneCharacter" => ("x", "x"),
                "Exactly500" => (FiveHundred, FiveHundred),
                "Exactly500WithSpacesAround" => ($"  \t{FiveHundred}\n  ", FiveHundred),
                "InnerWhitespaceKept" => ("  first line\nsecond  line  ", "first line\nsecond  line"),
                "Macedonian" => ("  Злоупотреба на групата  ", "Злоупотреба на групата"),
                _ => throw new ArgumentOutOfRangeException(nameof(reasonCase), reasonCase, "Not one of the reason cases."),
            };
        }
    }
}
