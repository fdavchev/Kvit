using System.Text.Json;
using Kvit.Domain.Entities;
using Xunit;

namespace Kvit.Domain.Tests.Entities
{
    public static class ActivityEventData
    {
        public static string? Name(ActivityEvent activityEvent)
        {
            return Read(activityEvent).GetProperty("name").GetString();
        }

        public static string? ClaimedName(ActivityEvent activityEvent)
        {
            return Read(activityEvent).GetProperty("claimedName").GetString();
        }

        public static List<string> PropertyNames(ActivityEvent activityEvent)
        {
            return [.. Read(activityEvent).EnumerateObject().Select(property => property.Name)];
        }

        private static JsonElement Read(ActivityEvent activityEvent)
        {
            Assert.NotNull(activityEvent.Data);

            return JsonDocument.Parse(activityEvent.Data).RootElement;
        }
    }
}
