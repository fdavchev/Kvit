using System.Net;
using System.Text.Json;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Auth;
using Xunit;

namespace Kvit.Api.Tests.Auth
{
    public class RegisterTests(AuthApp _app) : IClassFixture<AuthApp>
    {
        [Fact]
        public async Task Register_ValidData_Answers200WithExactlyTheSixAccountFields()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.RegisterAsync(client, form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            string[] propertyNames = [.. body.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)];
            string[] expectedNames = ["displayName", "email", "id", "language", "mustChangePassword", "timeZone"];
            Assert.Equal(expectedNames, propertyNames);
        }

        [Fact]
        public async Task Register_ValidData_AnswersTheStoredAccountValues()
        {
            RegistrationForm form = RegistrationForm.Valid() with { DisplayName = "Ана Петровска", Language = "mk", TimeZone = "America/New_York" };
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.RegisterAsync(client, form);

            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            AppUser user = await _app.FindUserAsync(form.Email);
            Assert.Equal(user.Id, body.GetProperty("id").GetGuid());
            Assert.Equal("Ана Петровска", body.GetProperty("displayName").GetString());
            Assert.Equal(form.Email, body.GetProperty("email").GetString());
            Assert.Equal("mk", body.GetProperty("language").GetString());
            Assert.Equal("America/New_York", body.GetProperty("timeZone").GetString());
            Assert.False(body.GetProperty("mustChangePassword").GetBoolean());
        }

        [Fact]
        public async Task Register_ValidData_StoresTheAccountFieldsAsSent()
        {
            RegistrationForm form = RegistrationForm.Valid() with { DisplayName = "Ана Петровска", Language = "mk", TimeZone = "America/New_York" };
            HttpClient client = _app.CreateClient();

            await AuthRequests.RegisterAsync(client, form);

            AppUser user = await _app.FindUserAsync(form.Email);
            Assert.Equal("Ана Петровска", user.DisplayName);
            Assert.Equal("mk", user.Language);
            Assert.Equal("America/New_York", user.TimeZone);
            Assert.False(user.IsTimeZoneManual);
            Assert.Equal(form.Email, user.UserName);
        }

        [Fact]
        public async Task Register_ValidData_StoresACreationTimeInUtcFromJustNow()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = _app.CreateClient();

            await AuthRequests.RegisterAsync(client, form);

            AppUser user = await _app.FindUserAsync(form.Email);
            Assert.Equal(TimeSpan.Zero, user.CreatedAt.Offset);
            Assert.InRange(DateTimeOffset.UtcNow - user.CreatedAt, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task Register_ValidData_StoresAHashAndNotThePassword()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = _app.CreateClient();

            await AuthRequests.RegisterAsync(client, form);

            AppUser user = await _app.FindUserAsync(form.Email);
            Assert.NotNull(user.PasswordHash);
            Assert.NotEqual(form.Password, user.PasswordHash);
        }

        [Fact]
        public async Task Register_ValidData_StartsWithACleanLockoutState()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = _app.CreateClient();

            await AuthRequests.RegisterAsync(client, form);

            AppUser user = await _app.FindUserAsync(form.Email);
            Assert.Equal(0, user.LockoutCount);
            Assert.Equal(0, user.AccessFailedCount);
            Assert.Null(user.LockoutEnd);
        }

        [Fact]
        public async Task Register_ValidData_RecordsExactlyOneSignedUpEventWithTheEmailMethod()
        {
            RegistrationForm form = RegistrationForm.Valid();
            HttpClient client = _app.CreateClient();
            DateOnly dayBefore = DateOnly.FromDateTime(DateTime.UtcNow);

            HttpResponseMessage response = await AuthRequests.RegisterAsync(client, form);

            DateOnly dayAfter = DateOnly.FromDateTime(DateTime.UtcNow);
            Guid userId = (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid();
            UsageEvent signedUp = Assert.Single(await _app.UsageEventsOfAsync(userId));
            Assert.Equal(UsageEventType.SignedUp, signedUp.Type);
            Assert.Equal("Email", signedUp.Detail);
            Assert.InRange(signedUp.OccurredOn, dayBefore, dayAfter);
        }

        [Theory]
        [InlineData("Ana", "Ana")]
        [InlineData("  Ana  ", "Ana")]
        [InlineData("A", "A")]
        [InlineData("Ана Петровска", "Ана Петровска")]
        public async Task Register_ValidDisplayName_IsReturnedAndStoredTrimmed(string displayName, string expectedName)
        {
            RegistrationForm form = RegistrationForm.Valid() with { DisplayName = displayName };
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.RegisterAsync(client, form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(expectedName, body.GetProperty("displayName").GetString());
            Assert.Equal(expectedName, (await _app.FindUserAsync(form.Email)).DisplayName);
        }

        [Fact]
        public async Task Register_DisplayNameOf60Characters_IsAccepted()
        {
            string displayName = new('a', 60);
            RegistrationForm form = RegistrationForm.Valid() with { DisplayName = displayName };
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.RegisterAsync(client, form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(displayName, (await _app.FindUserAsync(form.Email)).DisplayName);
        }

        [Fact]
        public async Task Register_DisplayNameOf60CharactersWithSurroundingSpaces_IsAcceptedBecauseLengthIsCountedAfterTrimming()
        {
            string displayName = new('a', 60);
            RegistrationForm form = RegistrationForm.Valid() with { DisplayName = $"  {displayName}  " };
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.RegisterAsync(client, form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(displayName, (await _app.FindUserAsync(form.Email)).DisplayName);
        }

        [Fact]
        public async Task Register_DisplayNameOf61Characters_IsRejectedAndCreatesNothing()
        {
            RegistrationForm form = RegistrationForm.Valid() with { DisplayName = new string('a', 61) };

            await AssertRejectedAsync(form, ResultCodes.AUTH_DISPLAY_NAME_INVALID);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        public async Task Register_BlankDisplayName_IsRejectedAndCreatesNothing(string displayName)
        {
            RegistrationForm form = RegistrationForm.Valid() with { DisplayName = displayName };

            await AssertRejectedAsync(form, ResultCodes.AUTH_DISPLAY_NAME_INVALID);
        }

        [Theory]
        [InlineData("")]
        [InlineData("not-an-email")]
        [InlineData("a@")]
        [InlineData("@b.com")]
        [InlineData("a b@c.com")]
        public async Task Register_InvalidEmail_IsRejectedAndCreatesNothing(string email)
        {
            RegistrationForm form = RegistrationForm.Valid() with { Email = email };

            await AssertRejectedAsync(form, ResultCodes.AUTH_EMAIL_INVALID);
        }

        [Fact]
        public async Task Register_EmailOf255Characters_IsRejectedAndCreatesNothing()
        {
            RegistrationForm form = RegistrationForm.Valid() with { Email = EmailOfLength(255) };

            await AssertRejectedAsync(form, ResultCodes.AUTH_EMAIL_INVALID);
        }

        [Fact]
        public async Task Register_EmailOf254Characters_IsAccepted()
        {
            RegistrationForm form = RegistrationForm.Valid() with { Email = EmailOfLength(254) };
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.RegisterAsync(client, form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Register_EmailWithSurroundingSpaces_IsReturnedAndStoredTrimmed()
        {
            string email = RegistrationForm.UniqueEmail();
            RegistrationForm form = RegistrationForm.Valid() with { Email = $"  {email}  " };
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.RegisterAsync(client, form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(email, body.GetProperty("email").GetString());
            AppUser user = await _app.FindUserAsync(email);
            Assert.Equal(email, user.UserName);
        }

        [Fact]
        public async Task Register_EmailInMixedCase_IsStoredAsTyped()
        {
            string email = $"Ana.{Guid.NewGuid():N}@Example.COM";
            RegistrationForm form = RegistrationForm.Valid() with { Email = email };
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.RegisterAsync(client, form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            AppUser user = await _app.FindUserAsync(email);
            Assert.Equal(email, user.Email);
            Assert.Equal(email, user.UserName);
        }

        [Fact]
        public async Task Register_EmailAlreadyUsed_IsRejectedAndCreatesNothing()
        {
            RegistrationForm form = RegistrationForm.Valid();
            await _app.CreateAccountAsync(form);

            await AssertRejectedAsync(form with { DisplayName = "Marko" }, ResultCodes.AUTH_EMAIL_TAKEN);
        }

        [Fact]
        public async Task Register_EmailAlreadyUsedInAnotherCase_IsRejectedAndCreatesNothing()
        {
            RegistrationForm form = RegistrationForm.Valid() with { Email = $"Ana.{Guid.NewGuid():N}@Example.com" };
            await _app.CreateAccountAsync(form);

            await AssertRejectedAsync(form with { Email = form.Email.ToLowerInvariant() }, ResultCodes.AUTH_EMAIL_TAKEN);
        }

        [Theory]
        [InlineData("sunce2026")]
        [InlineData("Sunce")]
        [InlineData("SunceSunce")]
        [InlineData("Short1A")]
        [InlineData("")]
        [InlineData("лозинка123")]
        public async Task Register_WeakPassword_IsRejectedAndCreatesNothing(string password)
        {
            RegistrationForm form = RegistrationForm.Valid() with { Password = password };

            await AssertRejectedAsync(form, ResultCodes.AUTH_PASSWORD_TOO_WEAK);
        }

        [Theory]
        [InlineData("Sunce2026")]
        [InlineData("Aaaaaaa1")]
        [InlineData("Лозинка123")]
        [InlineData("Sunce-2026 !")]
        public async Task Register_StrongPassword_IsAccepted(string password)
        {
            RegistrationForm form = RegistrationForm.Valid() with { Password = password };
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.RegisterAsync(client, form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("de")]
        [InlineData("EN")]
        [InlineData("")]
        public async Task Register_UnsupportedLanguage_IsRejectedAndCreatesNothing(string language)
        {
            RegistrationForm form = RegistrationForm.Valid() with { Language = language };

            await AssertRejectedAsync(form, ResultCodes.LANGUAGE_INVALID);
        }

        [Theory]
        [InlineData("en")]
        [InlineData("mk")]
        public async Task Register_SupportedLanguage_IsStoredAsSent(string language)
        {
            RegistrationForm form = RegistrationForm.Valid() with { Language = language };
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.RegisterAsync(client, form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(language, (await _app.FindUserAsync(form.Email)).Language);
        }

        [Theory]
        [InlineData("Mars/Olympus")]
        [InlineData("")]
        public async Task Register_InvalidTimeZone_IsRejectedAndCreatesNothing(string timeZone)
        {
            RegistrationForm form = RegistrationForm.Valid() with { TimeZone = timeZone };

            await AssertRejectedAsync(form, ResultCodes.TIME_ZONE_INVALID);
        }

        [Theory]
        [InlineData("Europe/Skopje")]
        [InlineData("America/New_York")]
        [InlineData("UTC")]
        public async Task Register_ValidTimeZone_IsStoredAsSent(string timeZone)
        {
            RegistrationForm form = RegistrationForm.Valid() with { TimeZone = timeZone };
            HttpClient client = _app.CreateClient();

            HttpResponseMessage response = await AuthRequests.RegisterAsync(client, form);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(timeZone, (await _app.FindUserAsync(form.Email)).TimeZone);
        }

        [Fact]
        public async Task Register_TwoSimultaneousRequestsForTheSameEmail_OneSucceedsAndTheOtherAnswersEmailTaken()
        {
            const int rounds = 5;

            for (int round = 0; round < rounds; round++)
            {
                RegistrationForm form = RegistrationForm.Valid();
                HttpClient firstClient = _app.CreateClient();
                HttpClient secondClient = _app.CreateClient();
                int eventsBefore = await _app.CountUsageEventsAsync();

                HttpResponseMessage[] responses = await Task.WhenAll(
                    AuthRequests.RegisterAsync(firstClient, form),
                    AuthRequests.RegisterAsync(secondClient, form));

                Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
                HttpResponseMessage rejected = Assert.Single(responses, response => response.StatusCode != HttpStatusCode.OK);
                await ProblemResponse.AssertAsync(rejected, HttpStatusCode.BadRequest, ResultCodes.AUTH_EMAIL_TAKEN);
                Assert.Equal(1, await _app.CountUsersWithEmailAsync(form.Email));
                Assert.Equal(eventsBefore + 1, await _app.CountUsageEventsAsync());
            }
        }

        private async Task AssertRejectedAsync(RegistrationForm form, string expectedErrorCode)
        {
            HttpClient client = _app.CreateClient();
            int usersBefore = await _app.CountUsersAsync();
            int eventsBefore = await _app.CountUsageEventsAsync();

            HttpResponseMessage response = await AuthRequests.RegisterAsync(client, form);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, expectedErrorCode);
            Assert.Null(AuthCookie.Find(response));
            Assert.Equal(usersBefore, await _app.CountUsersAsync());
            Assert.Equal(eventsBefore, await _app.CountUsageEventsAsync());
        }

        private static string EmailOfLength(int length)
        {
            string localPart = new('a', 64);
            string firstLabels = new string('b', 63) + "." + new string('b', 63) + ".";
            int lastLabelLength = length - localPart.Length - 1 - firstLabels.Length - ".com".Length;

            return $"{localPart}@{firstLabels}{new string('b', lastLabelLength)}.com";
        }
    }
}
