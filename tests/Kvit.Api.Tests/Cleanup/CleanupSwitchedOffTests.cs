using System.Net;
using Xunit;

namespace Kvit.Api.Tests.Cleanup
{
    public class CleanupSwitchedOffTests(CleanupApp app) : CleanupTestBase(app)
    {
        [Fact]
        public async Task Post_SecretNotConfigured_Answers404WithAnEmptyBody()
        {
            HttpResponseMessage response = await CleanupRequests.PostAsync(_app.CreateClientOfTheHostWithoutTheSecret(), CleanupApp.Secret);

            await CleanupRequests.AssertEmptyAnswerAsync(response, HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Post_SecretNotConfiguredAndNoHeader_Answers404WithAnEmptyBody()
        {
            HttpResponseMessage response = await CleanupRequests.PostWithoutSecretAsync(_app.CreateClientOfTheHostWithoutTheSecret());

            await CleanupRequests.AssertEmptyAnswerAsync(response, HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Post_SecretNotConfiguredAndAnEmptyHeader_Answers404WithAnEmptyBody()
        {
            HttpResponseMessage response = await CleanupRequests.PostAsync(_app.CreateClientOfTheHostWithoutTheSecret(), string.Empty);

            await CleanupRequests.AssertEmptyAnswerAsync(response, HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Post_SecretNotConfiguredWhileRowsAreDue_ChangesNothing()
        {
            await CleanupSteps.AddOneDueRowOfEveryKindAsync(_app);
            string before = await CleanupRows.SnapshotOfTheWholeDatabaseAsync(_app);

            await CleanupRequests.PostAsync(_app.CreateClientOfTheHostWithoutTheSecret(), CleanupApp.Secret);

            Assert.Equal(before, await CleanupRows.SnapshotOfTheWholeDatabaseAsync(_app));
        }
    }
}
