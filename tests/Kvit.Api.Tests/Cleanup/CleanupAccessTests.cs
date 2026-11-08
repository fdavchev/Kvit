using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.Cleanup
{
    public class CleanupAccessTests(CleanupApp app) : CleanupTestBase(app)
    {
        public static TheoryData<string> NearMissSecrets =>
        [
            CleanupApp.Secret + "x",
            CleanupApp.Secret[..^1],
            CleanupApp.Secret.ToUpperInvariant(),
            "x" + CleanupApp.Secret,
            "wrong-secret",
        ];

        public static TheoryData<string> OtherMethods => ["GET", "PUT", "DELETE", "PATCH"];

        [Fact]
        public async Task Post_RightSecretWithoutACookie_Answers200()
        {
            HttpResponseMessage response = await CleanupRequests.PostAsync(_app.CreateClientWithoutCookies(), CleanupApp.Secret);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Post_RightSecret_AnswersJson()
        {
            HttpResponseMessage response = await CleanupRequests.PostAsync(_app.CreateClientWithoutCookies(), CleanupApp.Secret);

            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Post_RightSecret_AnswersExactlyTheSixDocumentedFields()
        {
            HttpResponseMessage response = await CleanupRequests.PostAsync(_app.CreateClientWithoutCookies(), CleanupApp.Secret);

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(CleanupRequests.FieldNames, GroupRequests.PropertyNamesOf(body));
        }

        [Fact]
        public async Task Post_RightSecretAndNothingDue_AnswersSixZeros()
        {
            CleanupResult result = await _app.RunCleanupAsync();

            Assert.Equal(CleanupResult.Nothing, result);
        }

        [Theory]
        [InlineData("x-kvit-cleanup-secret")]
        [InlineData("X-KVIT-CLEANUP-SECRET")]
        public async Task Post_HeaderNameInAnotherCase_Answers200(string headerName)
        {
            HttpResponseMessage response = await CleanupRequests.PostWithHeaderAsync(_app.CreateClientWithoutCookies(), headerName, CleanupApp.Secret);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Post_SignedInPersonWithTheRightSecret_Answers200()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await CleanupRequests.PostAsync(user.Client, CleanupApp.Secret);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Post_SignedInPersonWithoutTheSecret_Answers403WithAnEmptyBody()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await CleanupRequests.PostWithoutSecretAsync(user.Client);

            await CleanupRequests.AssertEmptyAnswerAsync(response, HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Post_NoHeader_Answers403WithAnEmptyBody()
        {
            HttpResponseMessage response = await CleanupRequests.PostWithoutSecretAsync(_app.CreateClientWithoutCookies());

            await CleanupRequests.AssertEmptyAnswerAsync(response, HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Post_EmptyHeader_Answers403WithAnEmptyBody()
        {
            HttpResponseMessage response = await CleanupRequests.PostAsync(_app.CreateClientWithoutCookies(), string.Empty);

            await CleanupRequests.AssertEmptyAnswerAsync(response, HttpStatusCode.Forbidden);
        }

        [Theory]
        [MemberData(nameof(NearMissSecrets))]
        public async Task Post_WrongSecret_Answers403WithAnEmptyBody(string secret)
        {
            HttpResponseMessage response = await CleanupRequests.PostAsync(_app.CreateClientWithoutCookies(), secret);

            await CleanupRequests.AssertEmptyAnswerAsync(response, HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Post_RightSecretInTheQueryStringOnly_Answers403()
        {
            HttpResponseMessage response = await CleanupRequests.PostWithSecretInTheQueryAsync(_app.CreateClientWithoutCookies(), CleanupApp.Secret);

            await CleanupRequests.AssertEmptyAnswerAsync(response, HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Post_RightSecretAsABearerToken_Answers403()
        {
            HttpResponseMessage response = await CleanupRequests.PostWithHeaderAsync(_app.CreateClientWithoutCookies(), "Authorization", $"Bearer {CleanupApp.Secret}");

            await CleanupRequests.AssertEmptyAnswerAsync(response, HttpStatusCode.Forbidden);
        }

        [Theory]
        [MemberData(nameof(OtherMethods))]
        public async Task OtherMethod_WithTheRightSecret_IsNotSuccessful(string method)
        {
            HttpResponseMessage response = await CleanupRequests.SendAsync(_app.CreateClientWithoutCookies(), new HttpMethod(method), CleanupApp.Secret);

            Assert.False(response.IsSuccessStatusCode, $"{method} answered {(int)response.StatusCode}.");
        }

        [Fact]
        public async Task Get_WithTheRightSecretWhileRowsAreDue_ChangesNothing()
        {
            await CleanupSteps.AddOneDueRowOfEveryKindAsync(_app);
            string before = await CleanupRows.SnapshotOfTheWholeDatabaseAsync(_app);

            await CleanupRequests.SendAsync(_app.CreateClientWithoutCookies(), HttpMethod.Get, CleanupApp.Secret);

            Assert.Equal(before, await CleanupRows.SnapshotOfTheWholeDatabaseAsync(_app));
        }

        [Fact]
        public async Task Post_NoHeaderWhileRowsAreDue_ChangesNothing()
        {
            await CleanupSteps.AddOneDueRowOfEveryKindAsync(_app);
            string before = await CleanupRows.SnapshotOfTheWholeDatabaseAsync(_app);

            await CleanupRequests.PostWithoutSecretAsync(_app.CreateClientWithoutCookies());

            Assert.Equal(before, await CleanupRows.SnapshotOfTheWholeDatabaseAsync(_app));
        }

        [Fact]
        public async Task Post_WrongSecretWhileRowsAreDue_ChangesNothing()
        {
            await CleanupSteps.AddOneDueRowOfEveryKindAsync(_app);
            string before = await CleanupRows.SnapshotOfTheWholeDatabaseAsync(_app);

            await CleanupRequests.PostAsync(_app.CreateClientWithoutCookies(), "wrong-secret");

            Assert.Equal(before, await CleanupRows.SnapshotOfTheWholeDatabaseAsync(_app));
        }
    }
}
