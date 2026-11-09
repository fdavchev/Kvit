using System.Globalization;
using System.Text.Json;

namespace Kvit.Api.Tests.OwnerTools
{
    public sealed record StoredAction(
        Guid AdminUserId,
        string Action,
        Guid? TargetUserId,
        Guid? TargetGroupId,
        string TargetLabel,
        string Reason,
        DateTimeOffset CreatedAt)
    {
        public static StoredAction Parse(string rowAsJson)
        {
            using JsonDocument document = JsonDocument.Parse(rowAsJson);
            JsonElement row = document.RootElement;

            return new StoredAction(
                row.GetProperty("admin_user_id").GetGuid(),
                row.GetProperty("action").GetString()!,
                GuidOrNull(row, "target_user_id"),
                GuidOrNull(row, "target_group_id"),
                row.GetProperty("target_label").GetString()!,
                row.GetProperty("reason").GetString()!,
                DateTimeOffset.Parse(row.GetProperty("created_at").GetString()!, CultureInfo.InvariantCulture));
        }

        private static Guid? GuidOrNull(JsonElement row, string property)
        {
            JsonElement value = row.GetProperty(property);

            return value.ValueKind == JsonValueKind.Null ? null : value.GetGuid();
        }
    }
}
