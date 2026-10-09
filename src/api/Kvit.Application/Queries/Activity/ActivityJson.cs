using System.Text.Json;

namespace Kvit.Application.Queries.Activity
{
    public static class ActivityJson
    {
        public static JsonElement? Parse(string? storedJson)
        {
            if (storedJson is null)
            {
                return null;
            }

            using JsonDocument document = JsonDocument.Parse(storedJson);

            return document.RootElement.Clone();
        }
    }
}
