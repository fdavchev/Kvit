using System.Net;
using Kvit.Api.Tests.AccountDeletion;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Contracts.Auth;
using Kvit.Domain.Accounts;
using Kvit.Domain.Results;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class LogInSameMomentTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        private const string NewTimeZone = "Europe/Berlin";
        private const string NewPictureUrl = "https://lh3.googleusercontent.com/a/new-picture";
        private const string TimeZoneChanging = "OLD.time_zone IS DISTINCT FROM NEW.time_zone";

        [Fact]
        public async Task LogIn_TwoLogInsOfTheSameAccountAtTheSameMoment_BothAnswer200AndTheNewTimeZoneIsStored()
        {
            SignedInUser user = await _app.SignUpAsync();
            string stampBefore = (await GuestUpgradeChecks.UserValueAsync(_app, user.UserId, "concurrency_stamp"))!;
            await using InsertGate gate = await InsertGate.CloseUpdatesAsync(_app, "users", TimeZoneChanging);

            Task<HttpResponseMessage> first = AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, user.Form.Password, NewTimeZone);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, user.Form.Password, NewTimeZone);
            await gate.WaitUntilRequestsWaitForTheHeldRowAsync(1);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK], [.. responses.Select(response => response.StatusCode)]);
            Assert.Equal(NewTimeZone, await GuestUpgradeChecks.UserValueAsync(_app, user.UserId, "time_zone"));
            Assert.Equal(stampBefore, await GuestUpgradeChecks.UserValueAsync(_app, user.UserId, "concurrency_stamp"));
            Assert.All(responses, response => Assert.NotNull(AuthCookie.Find(response)));
        }

        [Fact]
        public async Task LogIn_AfterAWrongPassword_TwoRightLogInsAtTheSameMoment_BothAnswer200AndTheFailedCountIsZero()
        {
            SignedInUser user = await _app.SignUpAsync();
            HttpResponseMessage wrong = await AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, AuthRequests.WrongPassword);
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
            Assert.Equal("1", await GuestUpgradeChecks.UserValueAsync(_app, user.UserId, "access_failed_count"));
            await using InsertGate gate = await InsertGate.CloseUpdatesAsync(_app, "users", "OLD.access_failed_count > 0 AND NEW.access_failed_count = 0");

            Task<HttpResponseMessage> first = AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, user.Form.Password);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, user.Form.Password);
            await gate.WaitUntilRequestsWaitForTheHeldRowAsync(1);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK], [.. responses.Select(response => response.StatusCode)]);
            Assert.Equal("0", await GuestUpgradeChecks.UserValueAsync(_app, user.UserId, "access_failed_count"));
            Assert.All(responses, response => Assert.NotNull(AuthCookie.Find(response)));
        }

        [Fact]
        public async Task LogIn_TwoWrongPasswordsAtTheSameMoment_BothAnswer401()
        {
            SignedInUser user = await _app.SignUpAsync();
            await using InsertGate gate = await InsertGate.CloseUpdatesAsync(_app, "users", "NEW.access_failed_count > OLD.access_failed_count");

            Task<HttpResponseMessage> first = AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, AuthRequests.WrongPassword);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, AuthRequests.WrongPassword);
            await gate.WaitUntilRequestsWaitForTheHeldRowAsync(1);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized], [.. responses.Select(response => response.StatusCode)]);
        }

        [Fact]
        public async Task LogIn_TwoWrongPasswordsAtTheSameMoment_BothCountAsFailedTries()
        {
            SignedInUser user = await _app.SignUpAsync();
            await using InsertGate gate = await InsertGate.CloseUpdatesAsync(_app, "users", "NEW.access_failed_count > OLD.access_failed_count");

            Task<HttpResponseMessage> first = AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, AuthRequests.WrongPassword);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, AuthRequests.WrongPassword);
            await gate.WaitUntilRequestsWaitForTheHeldRowAsync(1);
            await gate.OpenAsync();
            await Task.WhenAll(first, second);

            Assert.Equal("2", await GuestUpgradeChecks.UserValueAsync(_app, user.UserId, "access_failed_count"));
        }

        [Fact]
        public async Task LogIn_TwoWrongPasswordsAtTheSameMomentOnTheLastFreeTry_BothAnswerLockedAndTheAccountIsLockedOnce()
        {
            SignedInUser user = await _app.SignUpAsync();
            await _app.ExecuteAsync(
                "UPDATE users SET access_failed_count = @count WHERE id = @id",
                new NpgsqlParameter("count", LockoutLadder.FailedTriesBeforeLock - 1),
                new NpgsqlParameter("id", user.UserId));
            await using InsertGate gate = await InsertGate.CloseUpdatesAsync(_app, "users", "NEW.lockout_end IS DISTINCT FROM OLD.lockout_end");

            Task<HttpResponseMessage> first = AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, AuthRequests.WrongPassword);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, AuthRequests.WrongPassword);
            await gate.WaitUntilRequestsWaitForTheHeldRowAsync(1);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            foreach (HttpResponseMessage response in responses)
            {
                await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.AUTH_LOCKED_OUT);
            }

            Assert.Equal("1|0|true", await GuestUpgradeChecks.UserValueAsync(_app, user.UserId, "concat_ws('|', lockout_count::text, access_failed_count::text, (lockout_end > now())::text)"));
        }

        [Fact]
        public async Task LogIn_WithALockoutCountAndANewTimeZone_ResetsTheCountAndStoresTheTimeZoneInOneWrite()
        {
            SignedInUser user = await _app.SignUpAsync();
            await _app.ExecuteAsync("UPDATE users SET lockout_count = 2 WHERE id = @id", new NpgsqlParameter("id", user.UserId));

            HttpResponseMessage response = await AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, user.Form.Password, NewTimeZone);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal($"0|{NewTimeZone}", await GuestUpgradeChecks.UserValueAsync(_app, user.UserId, "lockout_count || '|' || time_zone"));
        }

        [Fact]
        public async Task LogIn_WithAPendingDeletion_TwoLogInsAtTheSameMoment_BothAnswer200AndTheDeletionIsCancelled()
        {
            SignedInUser user = await _app.SignUpAsync();
            await DeletionSteps.RequestAsync(user.Client, DeletionRequests.KeepName);
            await using InsertGate gate = await InsertGate.CloseUpdatesAsync(_app, "users", "OLD.deletion_requested_at IS NOT NULL AND NEW.deletion_requested_at IS NULL");

            Task<HttpResponseMessage> first = AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, user.Form.Password);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, user.Form.Password);
            await gate.WaitUntilRequestsWaitForTheHeldRowAsync(1);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK], [.. responses.Select(response => response.StatusCode)]);
            Assert.Equal(StoredDeletion.None, await DeletionRows.ReadAsync(_app, user.UserId));
        }

        [Fact]
        public async Task GoogleLogIn_TwoLogInsWithANewPictureAtTheSameMoment_BothAnswer200AndThePictureIsStored()
        {
            GoogleIdentity identity = GoogleIdentities.Verified();
            await _app.CreateGoogleSignedUpClientAsync(identity);
            Guid userId = (await _app.FindUserAsync(identity.Email)).Id;
            string firstToken = _app.AddGoogleToken(identity with { PictureUrl = NewPictureUrl });
            string secondToken = _app.AddGoogleToken(identity with { PictureUrl = NewPictureUrl });
            await using InsertGate gate = await InsertGate.CloseUpdatesAsync(_app, "users", "OLD.google_picture_url IS DISTINCT FROM NEW.google_picture_url");

            Task<HttpResponseMessage> first = AuthRequests.GoogleLogInAsync(_app.CreateClient(), firstToken);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = AuthRequests.GoogleLogInAsync(_app.CreateClient(), secondToken);
            await gate.WaitUntilRequestsWaitForTheHeldRowAsync(1);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK], [.. responses.Select(response => response.StatusCode)]);
            Assert.Equal(NewPictureUrl, await GuestUpgradeChecks.UserValueAsync(_app, userId, "google_picture_url"));
        }

        [Fact]
        public async Task LogIn_WithNothingToChange_WritesNothingToTheAccount()
        {
            SignedInUser user = await _app.SignUpAsync();
            string before = (await GuestUpgradeChecks.UserValueAsync(_app, user.UserId, "users::text || '|' || users.xmin::text"))!;

            HttpResponseMessage response = await AuthRequests.LogInAsync(_app.CreateClient(), user.Form.Email, user.Form.Password, user.Form.TimeZone);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(before, await GuestUpgradeChecks.UserValueAsync(_app, user.UserId, "users::text || '|' || users.xmin::text"));
        }
    }
}
