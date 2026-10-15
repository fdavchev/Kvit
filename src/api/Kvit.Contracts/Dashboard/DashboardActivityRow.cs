using System.Text.Json;
using Kvit.Domain.Entities;

namespace Kvit.Contracts.Dashboard
{
    public sealed record DashboardActivityRow(
        Guid Id,
        ActivityEventType Type,
        Guid ActorUserId,
        string ActorName,
        Guid? ExpenseId,
        Guid? MemberId,
        JsonElement? Changes,
        JsonElement? Data,
        DateTimeOffset CreatedAt,
        Guid GroupId,
        string GroupName);
}
