using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.OwnerTools
{
    public class OwnerToolsAccessTests(OwnerToolsApp _app) : IClassFixture<OwnerToolsApp>
    {
        [Theory]
        [MemberData(nameof(OwnerToolsCalls.Names), MemberType = typeof(OwnerToolsCalls))]
        public async Task Call_Anonymous_Answers401(string name)
        {
            OwnerToolsCall call = OwnerToolsCalls.Named(name);
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await call.SendAsync(client, Guid.NewGuid(), Guid.NewGuid(), OwnerToolsRequests.DefaultReason);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(OwnerToolsCalls.Names), MemberType = typeof(OwnerToolsCalls))]
        public async Task Call_SignedInWithoutTheAdminRoleAndWithUnknownIds_Answers403NotNotFound(string name)
        {
            OwnerToolsCall call = OwnerToolsCalls.Named(name);
            SignedInUser person = await _app.SignUpAsync();

            HttpResponseMessage response = await call.SendAsync(person.Client, Guid.NewGuid(), Guid.NewGuid(), OwnerToolsRequests.DefaultReason);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(OwnerToolsCalls.NamesWithAReason), MemberType = typeof(OwnerToolsCalls))]
        public async Task Call_SignedInWithoutTheAdminRoleAndWithAnInvalidReason_Answers403NotTheValidationError(string name)
        {
            OwnerToolsCall call = OwnerToolsCalls.Named(name);
            SignedInUser person = await _app.SignUpAsync();
            SignedInUser target = await _app.SignUpAsync();

            HttpResponseMessage response = await call.SendAsync(person.Client, target.UserId, Guid.NewGuid(), string.Empty);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Null(await OwnerToolsRows.SuspendedAtAsync(_app, target.UserId));
        }

        [Theory]
        [MemberData(nameof(OwnerToolsCalls.Names), MemberType = typeof(OwnerToolsCalls))]
        public async Task Call_AdminWithATemporaryPassword_Answers403MustChangePassword(string name)
        {
            OwnerToolsCall call = OwnerToolsCalls.Named(name);
            ResetAccount account = await _app.CreateResetAccountAsync();
            await _app.MakeAdminAsync(account.Form.Email);
            HttpClient client = await _app.CreateLoggedInClientAsync(account.Form.Email, account.TemporaryPassword);

            HttpResponseMessage response = await call.SendAsync(client, Guid.NewGuid(), Guid.NewGuid(), OwnerToolsRequests.DefaultReason);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.AUTH_MUST_CHANGE_PASSWORD);
        }

        [Theory]
        [MemberData(nameof(OwnerToolsCalls.Names), MemberType = typeof(OwnerToolsCalls))]
        public async Task Call_Admin_AnswersTheSuccessStatus(string name)
        {
            OwnerToolsCall call = OwnerToolsCalls.Named(name);
            SignedInUser admin = await _app.SharedAdminAsync();
            SignedInUser target = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(await _app.SignUpAsync());

            HttpResponseMessage response = await call.SendAsync(admin.Client, target.UserId, groupId, OwnerToolsRequests.DefaultReason);

            Assert.Equal(call.SuccessStatus, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(OwnerToolsCalls.Names), MemberType = typeof(OwnerToolsCalls))]
        public async Task Call_AdminWhoIsSuspended_Answers403AccountSuspended(string name)
        {
            OwnerToolsCall call = OwnerToolsCalls.Named(name);
            SignedInUser admin = await _app.SignInAdminAsync();
            SignedInUser target = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(await _app.SignUpAsync());
            await OwnerToolsRows.SetSuspendedAtAsync(_app, admin.UserId, _app.Clock.GetUtcNow());
            _app.Clock.Advance(TimeSpan.FromSeconds(1));

            HttpResponseMessage response = await call.SendAsync(admin.Client, target.UserId, groupId, OwnerToolsRequests.DefaultReason);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, OwnerToolsRequests.AccountSuspendedCode);
            Assert.Null(await OwnerToolsRows.SuspendedAtAsync(_app, target.UserId));
            Assert.Null(await OwnerToolsRows.AdminClosedAtAsync(_app, groupId));
        }

        [Fact]
        public async Task Call_AdminWhoIsLoggedOut_Answers401()
        {
            SignedInUser admin = await _app.SignInAdminAsync();
            await AuthRequests.LogOutAsync(admin.Client);

            HttpResponseMessage response = await OwnerToolsRequests.PeopleAsync(admin.Client, "ab");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Call_PersonMadeAdminWhileSignedIn_Answers200OnTheNextCallWithoutLoggingInAgain()
        {
            SignedInUser person = await _app.SignUpAsync();
            Assert.Equal(HttpStatusCode.Forbidden, (await OwnerToolsRequests.PeopleAsync(person.Client, "ab")).StatusCode);

            await _app.MakeAdminAsync(person.Form.Email);
            _app.Clock.Advance(TimeSpan.FromSeconds(1));

            Assert.Equal(HttpStatusCode.OK, (await OwnerToolsRequests.PeopleAsync(person.Client, "ab")).StatusCode);
        }

        [Fact]
        public async Task Call_AdminWhoLosesTheRoleWhileSignedIn_Answers403OnTheNextCall()
        {
            SignedInUser admin = await _app.SignInAdminAsync();
            Assert.Equal(HttpStatusCode.OK, (await OwnerToolsRequests.PeopleAsync(admin.Client, "ab")).StatusCode);

            await _app.RevokeAdminAsync(admin.Form.Email);
            _app.Clock.Advance(TimeSpan.FromSeconds(1));

            Assert.Equal(HttpStatusCode.Forbidden, (await OwnerToolsRequests.PeopleAsync(admin.Client, "ab")).StatusCode);
        }

        [Fact]
        public async Task Call_RefusedToASignedInPersonWithoutTheRole_ChangesNothingAndWritesNoLogRow()
        {
            SignedInUser person = await _app.SignUpAsync();
            SignedInUser target = await _app.SignUpAsync();
            Guid groupId = await _app.CreateGroupAsync(await _app.SignUpAsync());
            int logRowsBefore = await OwnerToolsRows.CountActionsAsync(_app);

            await OwnerToolsRequests.SuspendAsync(person.Client, target.UserId);
            await OwnerToolsRequests.EraseNameAsync(person.Client, target.UserId);
            await OwnerToolsRequests.CloseAsync(person.Client, groupId);

            Assert.Null(await OwnerToolsRows.SuspendedAtAsync(_app, target.UserId));
            Assert.Equal(HttpStatusCode.OK, (await AuthRequests.GetMeAsync(target.Client)).StatusCode);
            Assert.Null(await OwnerToolsRows.AdminClosedAtAsync(_app, groupId));
            Assert.Equal(logRowsBefore, await OwnerToolsRows.CountActionsAsync(_app));
        }
    }
}
