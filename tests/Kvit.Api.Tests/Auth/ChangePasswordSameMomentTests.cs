using System.Net;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class ChangePasswordSameMomentTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        private const string NewPassword = "Novo-lozinka7";

        [Fact]
        public async Task ChangePassword_AfterAWrongPassword_TwoChangesAtTheSameMoment_TheFirstAnswers204AndTheSecondFindsTheCurrentPasswordWrong()
        {
            SignedInUser user = await _app.SignUpAsync();
            HttpResponseMessage wrong = await AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, AuthRequests.WrongPassword);
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
            await using InsertGate gate = await InsertGate.CloseUpdatesAsync(_app, "users", "OLD.access_failed_count > 0 AND NEW.access_failed_count = 0");

            Task<HttpResponseMessage> first = AuthRequests.ChangePasswordAsync(user.Client, user.Form.Password, NewPassword);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = AuthRequests.ChangePasswordAsync(user.Client, user.Form.Password, NewPassword);
            await gate.WaitUntilRequestsWaitForTheHeldRowAsync(1);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal(HttpStatusCode.NoContent, responses[0].StatusCode);
            await ProblemResponse.AssertAsync(responses[1], HttpStatusCode.BadRequest, ResultCodes.AUTH_CURRENT_PASSWORD_WRONG);
            Assert.Equal(HttpStatusCode.OK, (await AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, NewPassword)).StatusCode);
        }

        [Fact]
        public async Task ChangePassword_TwoChangesAtTheSameMoment_TheFirstAnswers204AndTheSecondFindsTheCurrentPasswordWrong()
        {
            SignedInUser user = await _app.SignUpAsync();
            await using InsertGate gate = await InsertGate.CloseUpdatesAsync(_app, "users", "OLD.password_hash IS DISTINCT FROM NEW.password_hash");

            Task<HttpResponseMessage> first = AuthRequests.ChangePasswordAsync(user.Client, user.Form.Password, NewPassword);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = AuthRequests.ChangePasswordAsync(user.Client, user.Form.Password, NewPassword);
            await gate.WaitUntilRequestsWaitForTheHeldRowAsync(1);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal(HttpStatusCode.NoContent, responses[0].StatusCode);
            await ProblemResponse.AssertAsync(responses[1], HttpStatusCode.BadRequest, ResultCodes.AUTH_CURRENT_PASSWORD_WRONG);
            Assert.Equal(HttpStatusCode.OK, (await AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, NewPassword)).StatusCode);
        }
    }
}
