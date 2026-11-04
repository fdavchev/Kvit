using System.Text.Json;

namespace Kvit.Api.Tests.AdminStats
{
    public static class StatsJson
    {
        public static int Int(JsonElement stats, string section, string property)
        {
            return stats.GetProperty(section).GetProperty(property).GetInt32();
        }

        public static List<int> Ints(JsonElement stats, string section, string property)
        {
            return [.. stats.GetProperty(section).GetProperty(property).EnumerateArray().Select(item => item.GetInt32())];
        }

        public static bool IsNull(JsonElement stats, string section, string property)
        {
            return stats.GetProperty(section).GetProperty(property).ValueKind == JsonValueKind.Null;
        }

        public static List<string> Buckets(JsonElement stats)
        {
            return [.. stats.GetProperty("buckets").EnumerateArray().Select(item => item.GetString()!)];
        }

        public static string Text(JsonElement stats, string property)
        {
            return stats.GetProperty(property).GetString()!;
        }

        public static bool IsNull(JsonElement stats, string property)
        {
            return stats.GetProperty(property).ValueKind == JsonValueKind.Null;
        }
    }
}
