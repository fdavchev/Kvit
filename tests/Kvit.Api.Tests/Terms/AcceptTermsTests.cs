using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Terms
{
    public class AcceptTermsTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string FreeName = "Marko";
        private static readonly DateTimeOffset OldAcceptedTime = new(2025, 1, 1, 8, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset AcceptTime = new(2026, 11, 4, 14, 45, 30, TimeSpan.Zero);

        [Fact]
        public async Task Accept_UserWithAnOlderVersion_Answers204WithNoBodyAndStoresTheCurrentVersionAndTheClockTime()
        {
            SignedInUser user = await _app.SignUpAsync();
            await TermsRows.SetAsync(_app, user.UserId, TermsRequests.OlderVersion, OldAcceptedTime);
            _app.Clock.SetTo(AcceptTime);

            HttpResponseMessage response = await TermsRequests.AcceptAsync(user.Client);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Empty(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Equal(new StoredTerms(TermsRequests.CurrentVersion, AcceptTime), await TermsRows.ReadAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Accept_UserWhoseTermsColumnsAreNull_Answers204AndStoresTheCurrentVersionAndTheClockTime()
        {
            SignedInUser user = await _app.SignUpAsync();
            await TermsRows.ClearAsync(_app, user.UserId);
            _app.Clock.SetTo(AcceptTime);

            HttpResponseMessage response = await TermsRequests.AcceptAsync(user.Client);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(new StoredTerms(TermsRequests.CurrentVersion, AcceptTime), await TermsRows.ReadAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Accept_ThenMe_AnswersTheCurrentVersionUpToDate()
        {
            SignedInUser user = await _app.SignUpAsync();
            await TermsRows.SetAsync(_app, user.UserId, TermsRequests.OlderVersion, OldAcceptedTime);
            Assert.False((await TermsRequests.ReadFieldsAsync(await AuthRequests.GetMeAsync(user.Client))).UpToDate);

            await TermsRequests.AcceptAsync(user.Client);

            Assert.Equal(TermsFields.Current, await TermsRequests.ReadFieldsAsync(await AuthRequests.GetMeAsync(user.Client)));
        }

        [Fact]
        public async Task Accept_Twice_Answers204BothTimesAndKeepsTheCurrentVersion()
        {
            SignedInUser user = await _app.SignUpAsync();
            await TermsRows.ClearAsync(_app, user.UserId);

            HttpResponseMessage first = await TermsRequests.AcceptAsync(user.Client);
            HttpResponseMessage second = await TermsRequests.AcceptAsync(user.Client);

            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
            StoredTerms stored = await TermsRows.ReadAsync(_app, user.UserId);
            Assert.Equal(TermsRequests.CurrentVersion, stored.Version);
            Assert.NotNull(stored.AcceptedAt);
        }

        [Fact]
        public async Task Accept_Guest_Answers204AndStoresTheCurrentVersionAndTheClockTime()
        {
            (_, GuestMember guest) = await GuestSteps.CreateExpenseGroupWithGuestAsync(_app, FreeName);
            await TermsRows.SetAsync(_app, guest.UserId, TermsRequests.OlderVersion, OldAcceptedTime);
            _app.Clock.SetTo(AcceptTime);

            HttpResponseMessage response = await TermsRequests.AcceptAsync(guest.Client);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(new StoredTerms(TermsRequests.CurrentVersion, AcceptTime), await TermsRows.ReadAsync(_app, guest.UserId));
        }

        [Fact]
        public async Task Accept_OneUser_LeavesTheTermsOfAnotherUserUnchanged()
        {
            SignedInUser accepting = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync();
            await TermsRows.SetAsync(_app, accepting.UserId, TermsRequests.OlderVersion, OldAcceptedTime);
            await TermsRows.SetAsync(_app, other.UserId, TermsRequests.OlderVersion, OldAcceptedTime);
            _app.Clock.SetTo(AcceptTime);

            await TermsRequests.AcceptAsync(accepting.Client);

            Assert.Equal(new StoredTerms(TermsRequests.CurrentVersion, AcceptTime), await TermsRows.ReadAsync(_app, accepting.UserId));
            Assert.Equal(new StoredTerms(TermsRequests.OlderVersion, OldAcceptedTime), await TermsRows.ReadAsync(_app, other.UserId));
        }

        [Fact]
        public async Task Accept_Anonymous_Answers401()
        {
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await TermsRequests.AcceptAsync(client);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Accept_WithATemporaryPassword_Answers403MustChangePasswordAndKeepsTheTermsUnchanged()
        {
            ResetAccount account = await _app.CreateResetAccountAsync();
            Guid userId = (await _app.FindUserAsync(account.Form.Email)).Id;
            await TermsRows.SetAsync(_app, userId, TermsRequests.OlderVersion, OldAcceptedTime);
            HttpClient client = await _app.CreateLoggedInClientAsync(account.Form.Email, account.TemporaryPassword);

            HttpResponseMessage response = await TermsRequests.AcceptAsync(client);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.AUTH_MUST_CHANGE_PASSWORD);
            Assert.Equal(new StoredTerms(TermsRequests.OlderVersion, OldAcceptedTime), await TermsRows.ReadAsync(_app, userId));
        }

        [Fact]
        public async Task Accept_AsAGetRequest_Answers405AndChangesNothing()
        {
            SignedInUser user = await _app.SignUpAsync();
            await TermsRows.SetAsync(_app, user.UserId, TermsRequests.OlderVersion, OldAcceptedTime);

            HttpResponseMessage response = await user.Client.GetAsync(TermsRequests.AcceptPath, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
            Assert.Equal(new StoredTerms(TermsRequests.OlderVersion, OldAcceptedTime), await TermsRows.ReadAsync(_app, user.UserId));
        }
    }
}
