using System.Globalization;
using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.AccountDeletion;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.OwnerTools
{
    public class OwnerToolsPeopleSearchTests(OwnerToolsApp _app) : IClassFixture<OwnerToolsApp>
    {
        private static readonly DateTimeOffset Newest = new(2099, 1, 1, 0, 0, 0, TimeSpan.Zero);

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("a")]
        [InlineData(" a ")]
        [InlineData("\t")]
        [InlineData("  n  ")]
        public async Task Search_QueryShorterThanTwoCharactersAfterTrimming_AnswersAnEmptyList(string query)
        {
            await PersonAsync("Ana Nikolovska");

            JsonElement people = await SearchAsync(query);

            Assert.Equal(0, people.GetArrayLength());
        }

        [Fact]
        public async Task Search_WithoutAQuery_AnswersAnEmptyList()
        {
            await PersonAsync("Ana Nikolovska");

            JsonElement people = await SearchAsync(null);

            Assert.Equal(0, people.GetArrayLength());
        }

        [Fact]
        public async Task Search_TwoCharactersAfterTrimming_FindsThePersonWhoseNameContainsThem()
        {
            string token = OwnerToolsSteps.UniqueToken();
            SignedInUser person = await PersonAsync($"Qz{token}");

            List<Guid> ids = OwnerToolsRequests.IdsOf(await SearchAsync("  qz  "));

            Assert.Contains(person.UserId, ids);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Search_ExactId_AnswersOnlyThatPerson(bool upperCase)
        {
            SignedInUser person = await PersonAsync("Marija Stojanovska");
            await PersonAsync("Marija Stojanovska");
            string query = upperCase ? person.UserId.ToString().ToUpperInvariant() : person.UserId.ToString();

            List<Guid> ids = OwnerToolsRequests.IdsOf(await SearchAsync(query));

            Assert.Equal([person.UserId], ids);
        }

        [Fact]
        public async Task Search_IdOfNoOne_AnswersAnEmptyList()
        {
            JsonElement people = await SearchAsync(Guid.NewGuid().ToString());

            Assert.Equal(0, people.GetArrayLength());
        }

        [Theory]
        [InlineData("start")]
        [InlineData("middle")]
        [InlineData("upper")]
        [InlineData("full")]
        public async Task Search_PartOfTheEmail_FindsThePerson(string part)
        {
            SignedInUser person = await PersonAsync("Dragan Ilievski");
            string email = person.Form.Email;
            string query = part switch
            {
                "start" => email[..12],
                "middle" => email.Substring(10, 10),
                "upper" => email[..12].ToUpperInvariant(),
                _ => email,
            };

            List<Guid> ids = OwnerToolsRequests.IdsOf(await SearchAsync(query));

            Assert.Contains(person.UserId, ids);
        }

        [Fact]
        public async Task Search_PartOfOnePersonsEmail_DoesNotAnswerAnotherPerson()
        {
            SignedInUser first = await PersonAsync("Dragan Ilievski");
            SignedInUser second = await PersonAsync("Dragan Ilievski");

            List<Guid> ids = OwnerToolsRequests.IdsOf(await SearchAsync(first.Form.Email[..16]));

            Assert.Equal([first.UserId], ids);
            Assert.DoesNotContain(second.UserId, ids);
        }

        [Theory]
        [InlineData("lower")]
        [InlineData("upper")]
        [InlineData("mixed")]
        public async Task Search_PartOfTheName_IgnoresCase(string casing)
        {
            string token = OwnerToolsSteps.UniqueToken();
            SignedInUser person = await PersonAsync($"Pera{token}");
            string query = casing switch
            {
                "lower" => $"pera{token}",
                "upper" => $"PERA{token}".ToUpperInvariant(),
                _ => $"pErA{token}",
            };

            List<Guid> ids = OwnerToolsRequests.IdsOf(await SearchAsync(query));

            Assert.Equal([person.UserId], ids);
        }

        [Fact]
        public async Task Search_PartInTheMiddleOfTheName_FindsThePerson()
        {
            string token = OwnerToolsSteps.UniqueToken();
            SignedInUser person = await PersonAsync($"Pera {token} Petrovski");

            List<Guid> ids = OwnerToolsRequests.IdsOf(await SearchAsync($"{token[3..9]}"));

            Assert.Equal([person.UserId], ids);
        }

        [Fact]
        public async Task Search_PartOfTheNameWithASpaceInside_FindsThePerson()
        {
            string token = OwnerToolsSteps.UniqueToken();
            SignedInUser person = await PersonAsync($"Pera {token} Petrovski");

            List<Guid> ids = OwnerToolsRequests.IdsOf(await SearchAsync($"{token} Petrov"));

            Assert.Equal([person.UserId], ids);
        }

        [Fact]
        public async Task Search_QuerySurroundedBySpaces_IsTrimmedBeforeMatching()
        {
            string token = OwnerToolsSteps.UniqueToken();
            SignedInUser person = await PersonAsync($"Pera {token}");

            List<Guid> ids = OwnerToolsRequests.IdsOf(await SearchAsync($"   {token}   "));

            Assert.Equal([person.UserId], ids);
        }

        [Fact]
        public async Task Search_PersonWhoseNameLacksTheQuery_IsNotAnswered()
        {
            string token = OwnerToolsSteps.UniqueToken();
            await PersonAsync($"Pera {OwnerToolsSteps.UniqueToken()}");
            SignedInUser person = await PersonAsync($"Pera {token}");

            List<Guid> ids = OwnerToolsRequests.IdsOf(await SearchAsync(token));

            Assert.Equal([person.UserId], ids);
        }

        [Fact]
        public async Task Search_PercentSign_IsTakenLiterallyAndMatchesNobody()
        {
            string token = OwnerToolsSteps.UniqueToken();
            await PersonAsync($"Pera {token}");

            JsonElement people = await SearchAsync($"%{token}");

            Assert.Equal(0, people.GetArrayLength());
        }

        [Fact]
        public async Task Search_PercentSignAlone_AnswersAnEmptyListNotEveryone()
        {
            await PersonAsync("Pera Petrovski");

            JsonElement people = await SearchAsync("%%");

            Assert.Equal(0, people.GetArrayLength());
        }

        [Fact]
        public async Task Search_UnderscoreInTheQuery_IsTakenLiterallyNotAsAnySingleCharacter()
        {
            string token = OwnerToolsSteps.UniqueToken();
            SignedInUser withUnderscore = await PersonAsync($"a_b{token}");
            SignedInUser withLetter = await PersonAsync($"axb{token}");

            List<Guid> ids = OwnerToolsRequests.IdsOf(await SearchAsync($"a_b{token}"));

            Assert.Equal([withUnderscore.UserId], ids);
            Assert.DoesNotContain(withLetter.UserId, ids);
        }

        [Fact]
        public async Task Search_MoreThanTenMatches_AnswersTheNewestTenNewestFirst()
        {
            string token = OwnerToolsSteps.UniqueToken();
            List<SignedInUser> people = [];
            for (int index = 0; index < 12; index++)
            {
                people.Add(await PersonAsync($"Group {token} {index:00}", Newest.AddMinutes(index)));
            }

            List<Guid> ids = OwnerToolsRequests.IdsOf(await SearchAsync(token));

            List<Guid> expected = [.. people.AsEnumerable().Reverse().Take(OwnerToolsRequests.PeopleMaxCount).Select(person => person.UserId)];
            Assert.Equal(expected, ids);
        }

        [Fact]
        public async Task Search_ExactlyTenMatches_AnswersAllTen()
        {
            string token = OwnerToolsSteps.UniqueToken();
            List<Guid> expected = [];
            for (int index = 0; index < OwnerToolsRequests.PeopleMaxCount; index++)
            {
                expected.Insert(0, (await PersonAsync($"Group {token} {index:00}", Newest.AddMinutes(index))).UserId);
            }

            List<Guid> ids = OwnerToolsRequests.IdsOf(await SearchAsync(token));

            Assert.Equal(expected, ids);
        }

        [Fact]
        public async Task Search_TwoMatches_AnswersTheMoreRecentlyCreatedFirst()
        {
            string token = OwnerToolsSteps.UniqueToken();
            SignedInUser older = await PersonAsync($"Old {token}", Newest.AddDays(-5));
            SignedInUser newer = await PersonAsync($"New {token}", Newest.AddDays(-1));

            List<Guid> ids = OwnerToolsRequests.IdsOf(await SearchAsync(token));

            Assert.Equal([newer.UserId, older.UserId], ids);
        }

        [Fact]
        public async Task Search_Person_AnswersExactlyTheTenDocumentedFields()
        {
            string token = OwnerToolsSteps.UniqueToken();
            await PersonAsync($"Pera {token}");

            JsonElement people = await SearchAsync(token);

            Assert.Equal(OwnerToolsRequests.PersonFieldNames, GroupRequests.PropertyNamesOf(people[0]));
        }

        [Fact]
        public async Task Search_Person_AnswersTheirIdNameEmailAndCreationTime()
        {
            string token = OwnerToolsSteps.UniqueToken();
            DateTimeOffset createdAt = Newest.AddHours(7);
            SignedInUser person = await PersonAsync($"Pera {token}", createdAt);

            JsonElement item = Assert.Single((await SearchAsync(token)).EnumerateArray());

            Assert.Equal(person.UserId, item.GetProperty("id").GetGuid());
            Assert.Equal($"Pera {token}", item.GetProperty("displayName").GetString());
            Assert.Equal(person.Form.Email, item.GetProperty("email").GetString());
            Assert.Equal(createdAt, DateTimeOffset.Parse(item.GetProperty("createdAt").GetString()!, CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task Search_Person_IsFoundByEmailAndByNameInTheSameAnswer()
        {
            string token = OwnerToolsSteps.UniqueToken();
            SignedInUser byName = await PersonAsync($"Pera {token}", Newest.AddMinutes(1));
            SignedInUser byEmail = await PersonAsync("Someone Else", Newest.AddMinutes(2));
            await OwnerToolsRows.SetEmailAsync(_app, byEmail.UserId, $"x{token}@example.com");

            List<Guid> ids = OwnerToolsRequests.IdsOf(await SearchAsync(token));

            Assert.Equal([byEmail.UserId, byName.UserId], ids);
        }

        [Fact]
        public async Task Search_WritesNoLogRowAndChangesNoPerson()
        {
            string token = OwnerToolsSteps.UniqueToken();
            SignedInUser person = await PersonAsync($"Pera {token}");
            string before = await DeletionRows.SnapshotOfUserRowAsync(_app, person.UserId);
            int logRowsBefore = await OwnerToolsRows.CountActionsAsync(_app);

            await SearchAsync(token);
            await SearchAsync(person.UserId.ToString());

            Assert.Equal(before, await DeletionRows.SnapshotOfUserRowAsync(_app, person.UserId));
            Assert.Equal(logRowsBefore, await OwnerToolsRows.CountActionsAsync(_app));
        }

        [Fact]
        public async Task Search_ByTheAdminThemselves_AnswersTheAdminToo()
        {
            SignedInUser admin = await _app.SharedAdminAsync();

            List<Guid> ids = OwnerToolsRequests.IdsOf(await SearchAsync(admin.UserId.ToString()));

            Assert.Equal([admin.UserId], ids);
        }

        [Fact]
        public async Task Search_Answer_IsJsonWithAnArrayAtTheTop()
        {
            SignedInUser admin = await _app.SharedAdminAsync();

            HttpResponseMessage response = await OwnerToolsRequests.PeopleAsync(admin.Client, "zz");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(JsonValueKind.Array, (await AuthRequests.ReadJsonAsync(response)).ValueKind);
        }

        private async Task<SignedInUser> PersonAsync(string displayName, DateTimeOffset? createdAt = null)
        {
            SignedInUser person = await _app.SignUpAsync(displayName);
            await OwnerToolsRows.SetCreatedAtAsync(_app, person.UserId, createdAt ?? Newest);

            return person;
        }

        private async Task<JsonElement> SearchAsync(string? query)
        {
            SignedInUser admin = await _app.SharedAdminAsync();

            return await OwnerToolsRequests.ReadPeopleAsync(admin.Client, query);
        }
    }
}
