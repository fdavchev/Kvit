using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Export
{
    public class ExportAccessTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Export_Anonymous_Answers401()
        {
            HttpClient anonymous = _app.CreateClient();

            HttpResponseMessage response = await ExportRequests.GetAsync(anonymous);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Null(response.Headers.Location);
        }

        [Fact]
        public async Task Export_AfterLogOut_Answers401()
        {
            SignedInUser user = await _app.SignUpAsync();
            await AuthRequests.LogOutAsync(user.Client);

            HttpResponseMessage response = await ExportRequests.GetAsync(user.Client);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Export_WithATemporaryPassword_Answers403MustChangePassword()
        {
            ResetAccount account = await _app.CreateResetAccountAsync();
            HttpClient client = await _app.CreateLoggedInClientAsync(account.Form.Email, account.TemporaryPassword);

            HttpResponseMessage response = await ExportRequests.GetAsync(client);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.AUTH_MUST_CHANGE_PASSWORD);
        }

        [Fact]
        public async Task Export_NormalAccount_Answers200()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await ExportRequests.GetAsync(user.Client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Export_Guest_Answers200()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, "Marko");

            HttpResponseMessage response = await ExportRequests.GetAsync(guest.Client);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
