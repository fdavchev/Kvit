using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Kvit.Api.Tests.Export
{
    public sealed record ExportBody(string Raw, JsonElement Root)
    {
        public JsonElement Profile => Root.GetProperty("profile");

        public JsonElement PersonalBudget => Root.GetProperty("personalBudget");

        public List<JsonElement> Groups => ListOf(Root, "groups");

        public static async Task<ExportBody> ReadAsync(HttpResponseMessage response)
        {
            string raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200 from {ExportRequests.Path}, got {(int)response.StatusCode} {response.StatusCode}: {raw}");
            using JsonDocument document = JsonDocument.Parse(raw);
            return new ExportBody(raw, document.RootElement.Clone());
        }

        public static List<JsonElement> ListOf(JsonElement parent, string propertyName)
        {
            Assert.True(parent.TryGetProperty(propertyName, out JsonElement list), $"The export has no '{propertyName}' property: {parent}");
            Assert.Equal(JsonValueKind.Array, list.ValueKind);

            return [.. list.EnumerateArray()];
        }

        public static JsonElement WithId(List<JsonElement> items, Guid id, string idProperty = "id")
        {
            return Assert.Single(items, item => item.GetProperty(idProperty).GetGuid() == id);
        }

        public static List<Guid> IdsOf(List<JsonElement> items, string idProperty = "id")
        {
            return [.. items.Select(item => item.GetProperty(idProperty).GetGuid())];
        }

        public static string? StringOrNull(JsonElement parent, string propertyName)
        {
            JsonElement value = parent.GetProperty(propertyName);

            return value.ValueKind == JsonValueKind.Null ? null : value.GetString();
        }

        public static Guid? GuidOrNull(JsonElement parent, string propertyName)
        {
            JsonElement value = parent.GetProperty(propertyName);

            return value.ValueKind == JsonValueKind.Null ? null : value.GetGuid();
        }

        public JsonElement GroupWithId(Guid groupId)
        {
            return WithId(Groups, groupId);
        }

        public void AssertDoesNotMention(string text)
        {
            Assert.DoesNotContain(text, Raw, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(text, AllDecodedText(), StringComparison.OrdinalIgnoreCase);
        }

        private string AllDecodedText()
        {
            StringBuilder builder = new();
            Collect(Root, builder);

            return builder.ToString();
        }

        private static void Collect(JsonElement element, StringBuilder builder)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (JsonProperty property in element.EnumerateObject())
                    {
                        builder.Append(property.Name).Append('\n');
                        Collect(property.Value, builder);
                    }

                    break;
                case JsonValueKind.Array:
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        Collect(item, builder);
                    }

                    break;
                default:
                    builder.Append(element.ToString()).Append('\n');
                    break;
            }
        }
    }
}
