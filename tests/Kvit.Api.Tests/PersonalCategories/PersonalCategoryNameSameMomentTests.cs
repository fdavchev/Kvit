using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.PersonalCategories
{
    public class PersonalCategoryNameSameMomentTests(GroupsApp _app) : IClassFixture<GroupsApp>
    {
        [Theory]
        [InlineData("Pets", "pets")]
        [InlineData("Храна", "ХРАНА")]
        public async Task Create_TwoCategoriesWithTheSameNameAtTheSameMoment_SavesOneAndAnswers400NameTakenForTheOther(string firstName, string secondName)
        {
            SignedInUser user = await _app.SignUpAsync();
            await using InsertGate gate = await InsertGate.CloseAsync(_app, "categories");

            Task<HttpResponseMessage> first = PersonalCategoryRequests.CreateAsync(user.Client, firstName);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> second = PersonalCategoryRequests.CreateAsync(user.Client, secondName);
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(first, second);

            Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
            HttpResponseMessage refused = Assert.Single(responses, response => response.StatusCode != HttpStatusCode.OK);
            await ProblemResponse.AssertAsync(refused, HttpStatusCode.BadRequest, ResultCodes.CATEGORY_NAME_TAKEN);
            Assert.Single(await PersonalCategorySteps.CustomNamesAsync(user.Client));
        }

        [Fact]
        public async Task Update_RenameHeldWhileACategoryWithThatNameIsCreated_Answers400NameTakenAndKeepsTheOldName()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid kids = await PersonalCategorySteps.CreateAsync(user.Client, "Kids");
            await using InsertGate gate = await InsertGate.CloseUpdatesAsync(_app, "categories", $"OLD.id = '{kids}'");

            Task<HttpResponseMessage> rename = PersonalCategoryRequests.UpdateAsync(user.Client, kids, "Dogs");
            await gate.WaitUntilRequestsAreHeldAsync(1);
            HttpResponseMessage created = await PersonalCategoryRequests.CreateAsync(user.Client, "dogs");
            await gate.OpenAsync();
            HttpResponseMessage renamed = await rename;

            Assert.Equal(HttpStatusCode.OK, created.StatusCode);
            await ProblemResponse.AssertAsync(renamed, HttpStatusCode.BadRequest, ResultCodes.CATEGORY_NAME_TAKEN);
            Assert.Equal(["Kids", "dogs"], (await PersonalCategorySteps.CustomNamesAsync(user.Client)).Order(StringComparer.Ordinal));
        }

        [Fact]
        public async Task Unarchive_BringBackHeldWhileACategoryWithThatNameIsCreated_Answers400NameTakenAndStaysArchived()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateArchivedAsync(user.Client, "Pets");
            await using InsertGate gate = await InsertGate.CloseUpdatesAsync(_app, "categories", $"OLD.id = '{pets}'");

            Task<HttpResponseMessage> bringBack = PersonalCategoryRequests.UnarchiveAsync(user.Client, pets);
            await gate.WaitUntilRequestsAreHeldAsync(1);
            HttpResponseMessage created = await PersonalCategoryRequests.CreateAsync(user.Client, "PETS");
            await gate.OpenAsync();
            HttpResponseMessage broughtBack = await bringBack;

            Assert.Equal(HttpStatusCode.OK, created.StatusCode);
            await ProblemResponse.AssertAsync(broughtBack, HttpStatusCode.BadRequest, ResultCodes.CATEGORY_NAME_TAKEN);
            Assert.NotNull(await PersonalCategorySteps.ArchivedAtAsync(_app, pets));
        }

        [Fact]
        public async Task Create_SameNameAtTheSameMomentByTwoPeople_SavesBoth()
        {
            SignedInUser first = await _app.SignUpAsync();
            SignedInUser second = await _app.SignUpAsync("Bojan");
            await using InsertGate gate = await InsertGate.CloseAsync(_app, "categories");

            Task<HttpResponseMessage> firstCreate = PersonalCategoryRequests.CreateAsync(first.Client, "Pets");
            await gate.WaitUntilRequestsAreHeldAsync(1);
            Task<HttpResponseMessage> secondCreate = PersonalCategoryRequests.CreateAsync(second.Client, "Pets");
            await gate.WaitUntilRequestsAreHeldAsync(2);
            await gate.OpenAsync();
            HttpResponseMessage[] responses = await Task.WhenAll(firstCreate, secondCreate);

            Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK], [.. responses.Select(response => response.StatusCode)]);
        }
    }
}
