using System.Text.Json;

namespace Kvit.Api.Tests.Closing
{
    public static class ClosingJson
    {
        public static List<string> NamesOf(JsonElement members)
        {
            return [.. members.EnumerateArray().Select(member => member.GetProperty("name").GetString()!).Order(StringComparer.Ordinal)];
        }

        public static List<Guid> IdsOf(JsonElement members)
        {
            return [.. members.EnumerateArray().Select(member => member.GetProperty("id").GetGuid()).Order()];
        }
    }
}
