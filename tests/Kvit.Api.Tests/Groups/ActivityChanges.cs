using System.Text.Json;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public static class ActivityChanges
    {
        public static List<(string Field, string Old, string New)> Parse(string? changesJson)
        {
            Assert.NotNull(changesJson);
            JsonElement changes = JsonDocument.Parse(changesJson).RootElement;
            Assert.Equal(JsonValueKind.Array, changes.ValueKind);

            return [.. changes.EnumerateArray().Select(ParseEntry)];
        }

        private static (string Field, string Old, string New) ParseEntry(JsonElement entry)
        {
            string[] propertyNames = [.. entry.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)];
            Assert.Equal(["field", "new", "old"], propertyNames);

            return (entry.GetProperty("field").GetString()!, entry.GetProperty("old").GetString()!, entry.GetProperty("new").GetString()!);
        }
    }
}
