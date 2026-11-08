using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Contracts.Auth;
using Xunit;

namespace Kvit.Api.Tests.AccountDeletion
{
    public class CancelByGoogleLogInTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task GoogleLogIn_PendingAccount_Answers200WithDeletionCancelledTrue()
        {
            (GoogleIdentity identity, HttpClient client, _) = await SignUpAndRequestAsync(DeletionRequests.KeepName);

            HttpResponseMessage response = await AuthRequests.GoogleLogInAsync(_app.CreateClient(), _app.AddGoogleToken(identity));

            Assert.True(DeletionSteps.DeletionCancelledOf(await DeletionSteps.ReadLogInAsync(response)));
            Assert.Equal(HttpStatusCode.Unauthorized, (await AuthRequests.GetMeAsync(client)).StatusCode);
        }

        [Theory]
        [InlineData("KeepName")]
        [InlineData("FormerMember")]
        public async Task GoogleLogIn_PendingAccount_ClearsTheRequestAndTheChoice(string nameChoice)
        {
            (GoogleIdentity identity, _, Guid userId) = await SignUpAndRequestAsync(nameChoice);

            await DeletionSteps.ReadLogInAsync(await AuthRequests.GoogleLogInAsync(_app.CreateClient(), _app.AddGoogleToken(identity)));

            Assert.Equal(StoredDeletion.None, await DeletionRows.ReadAsync(_app, userId));
        }

        [Fact]
        public async Task GoogleLogIn_PendingAccountTwice_AnswersDeletionCancelledTrueOnlyTheFirstTime()
        {
            (GoogleIdentity identity, _, _) = await SignUpAndRequestAsync(DeletionRequests.KeepName);

            JsonElement first = await DeletionSteps.ReadLogInAsync(await AuthRequests.GoogleLogInAsync(_app.CreateClient(), _app.AddGoogleToken(identity)));
            JsonElement second = await DeletionSteps.ReadLogInAsync(await AuthRequests.GoogleLogInAsync(_app.CreateClient(), _app.AddGoogleToken(identity)));

            Assert.True(DeletionSteps.DeletionCancelledOf(first));
            Assert.False(DeletionSteps.DeletionCancelledOf(second));
        }

        [Fact]
        public async Task GoogleLogIn_AccountWithNoDeletion_AnswersDeletionCancelledFalse()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            await _app.CreateGoogleSignedUpClientAsync(identity);

            HttpResponseMessage response = await AuthRequests.GoogleLogInAsync(_app.CreateClient(), _app.AddGoogleToken(identity));

            Assert.False(DeletionSteps.DeletionCancelledOf(await DeletionSteps.ReadLogInAsync(response)));
        }

        [Fact]
        public async Task GoogleLogIn_PendingAccount_GivesANewSessionThatWorks()
        {
            (GoogleIdentity identity, _, _) = await SignUpAndRequestAsync(DeletionRequests.FormerMember);
            HttpClient newDevice = _app.CreateClient();
            await DeletionSteps.ReadLogInAsync(await AuthRequests.GoogleLogInAsync(newDevice, _app.AddGoogleToken(identity)));

            HttpResponseMessage me = await AuthRequests.GetMeAsync(newDevice);

            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        }

        private async Task<(GoogleIdentity Identity, HttpClient Client, Guid UserId)> SignUpAndRequestAsync(string nameChoice)
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            HttpClient client = await _app.CreateGoogleSignedUpClientAsync(identity);
            Guid userId = (await _app.FindUserAsync(identity.Email)).Id;
            await DeletionSteps.RequestAsync(client, nameChoice);

            return (identity, client, userId);
        }
    }
}
