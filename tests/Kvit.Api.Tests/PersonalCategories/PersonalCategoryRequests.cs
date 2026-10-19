using System.Net.Http.Json;
using Xunit;

namespace Kvit.Api.Tests.PersonalCategories
{
    public static class PersonalCategoryRequests
    {
        public const string CategoriesPath = "/api/personal/categories";
        public const string DefaultName = "Pets";
        public const string DefaultEmoji = "\U0001F43E";
        public const string DefaultColor = "teal";

        public static readonly string[] RowFieldNames = ["archived", "color", "emoji", "id", "isCustom", "key", "name", "sortOrder"];

        public static string PathOf(Guid categoryId)
        {
            return $"{CategoriesPath}/{categoryId}";
        }

        public static Task<HttpResponseMessage> ListAsync(HttpClient client)
        {
            return client.GetAsync(CategoriesPath, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> ListIncludingArchivedAsync(HttpClient client)
        {
            return client.GetAsync($"{CategoriesPath}?includeArchived=true", TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> CreateAsync(
            HttpClient client,
            string name = DefaultName,
            string emoji = DefaultEmoji,
            string color = DefaultColor)
        {
            return client.PostAsJsonAsync(CategoriesPath, new { name, emoji, color }, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> UpdateAsync(
            HttpClient client,
            Guid categoryId,
            string name = DefaultName,
            string emoji = DefaultEmoji,
            string color = DefaultColor)
        {
            return client.PutAsJsonAsync(PathOf(categoryId), new { name, emoji, color }, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> ArchiveAsync(HttpClient client, Guid categoryId)
        {
            return client.PostAsync($"{PathOf(categoryId)}/archive", null, TestContext.Current.CancellationToken);
        }

        public static Task<HttpResponseMessage> UnarchiveAsync(HttpClient client, Guid categoryId)
        {
            return client.PostAsync($"{PathOf(categoryId)}/unarchive", null, TestContext.Current.CancellationToken);
        }

        public static TheoryData<string> AllEndpoints => ["list", "create", "update", "archive", "unarchive"];

        public static TheoryData<string> EndpointsWithAnId => ["update", "archive", "unarchive"];

        public static Task<HttpResponseMessage> SendAsync(HttpClient client, string endpoint, Guid categoryId)
        {
            return endpoint switch
            {
                "list" => ListAsync(client),
                "create" => CreateAsync(client),
                "update" => UpdateAsync(client, categoryId, "Renamed"),
                "archive" => ArchiveAsync(client, categoryId),
                "unarchive" => UnarchiveAsync(client, categoryId),
                _ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, "Not a personal category endpoint."),
            };
        }
    }
}
