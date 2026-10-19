using System.Net;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class UpgradeGuestConcurrentTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string GuestBecomingAnAccount = "OLD.is_guest AND NOT NEW.is_guest";
        private const string NewPassword = "Guest-Pass-2026";

        [Fact]
        public async Task UpgradeGuest_TwoGuestsWithTheSameEmailAtTheSameMoment_OneGetsTheEmailAndTheOtherAnswers400EmailTakenAndStaysAGuest()
        {
            (GuestMember first, GuestMember second) = await CreateTwoGuestsAsync();
            string email = RegistrationForm.UniqueEmail();

            HttpResponseMessage[] responses = await UpgradeBothAtOnceAsync(
                () => AuthRequests.UpgradeGuestAsync(first.Client, email, NewPassword),
                () => AuthRequests.UpgradeGuestAsync(second.Client, email, NewPassword));

            await AssertOneWonAndTheOtherStaysAGuestAsync(responses, first, second, ResultCodes.AUTH_EMAIL_TAKEN);
            Assert.Equal(1, await _app.CountUsersWithEmailAsync(email));
        }

        [Fact]
        public async Task GoogleUpgradeGuest_TwoGuestsWithTheSameGoogleAccountAtTheSameMoment_OneGetsItAndTheOtherAnswers400GoogleEmailTakenAndStaysAGuest()
        {
            (GuestMember first, GuestMember second) = await CreateTwoGuestsAsync();
            GoogleIdentity identity = GoogleIdentities.Verified();
            string firstToken = _app.AddGoogleToken(identity);
            string secondToken = _app.AddGoogleToken(identity);

            HttpResponseMessage[] responses = await UpgradeBothAtOnceAsync(
                () => AuthRequests.GoogleUpgradeGuestAsync(first.Client, firstToken),
                () => AuthRequests.GoogleUpgradeGuestAsync(second.Client, secondToken));

            GuestMember winner = await AssertOneWonAndTheOtherStaysAGuestAsync(responses, first, second, ResultCodes.AUTH_GOOGLE_EMAIL_TAKEN);
            Assert.Equal(1, await _app.CountUsersWithEmailAsync(identity.Email));
            Assert.Equal(winner.UserId, Assert.Single(await _app.GoogleLoginUserIdsAsync(identity.Subject)));
        }

        private async Task<(GuestMember First, GuestMember Second)> CreateTwoGuestsAsync()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko", "Petre");
            GuestMember first = await GuestSteps.TapFreeNameAsync(_app, group.GroupId, group.PlainRowIds[0]);
            GuestMember second = await GuestSteps.TapFreeNameAsync(_app, group.GroupId, group.PlainRowIds[1]);

            return (first, second);
        }

        private async Task<HttpResponseMessage[]> UpgradeBothAtOnceAsync(Func<Task<HttpResponseMessage>> firstUpgrade, Func<Task<HttpResponseMessage>> secondUpgrade)
        {
            await using InsertGate gate = await InsertGate.CloseUpdatesAsync(_app, "users", GuestBecomingAnAccount);

            Task<HttpResponseMessage> first = firstUpgrade();
            Task<HttpResponseMessage> second = secondUpgrade();
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();

            return await Task.WhenAll(first, second);
        }

        private async Task<GuestMember> AssertOneWonAndTheOtherStaysAGuestAsync(HttpResponseMessage[] responses, GuestMember first, GuestMember second, string expectedErrorCode)
        {
            Assert.Equal([HttpStatusCode.OK, HttpStatusCode.BadRequest], [.. responses.Select(response => response.StatusCode).Order()]);
            bool firstWon = responses[0].StatusCode == HttpStatusCode.OK;
            GuestMember winner = firstWon ? first : second;
            GuestMember loser = firstWon ? second : first;
            HttpResponseMessage refused = firstWon ? responses[1] : responses[0];
            await GuestUpgradeChecks.AssertRefusedAndStillAGuestAsync(_app, refused, loser, HttpStatusCode.BadRequest, expectedErrorCode);
            Assert.Equal("false", await GuestUpgradeChecks.UserValueAsync(_app, winner.UserId, "is_guest"));
            Assert.Equal(1, await GroupRows.CountUsageEventsAsync(_app, winner.UserId, "GuestUpgraded"));

            return winner;
        }
    }
}
