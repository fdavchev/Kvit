using System.Text.Json;
using Kvit.Domain.Entities;

namespace Kvit.Contracts.Activity
{
    public sealed record ActivityEventRow(
        Guid Id,
        ActivityEventType Type,
        Guid ActorUserId,
        string ActorName,
        Guid? ExpenseId,
        Guid? MemberId,
        JsonElement? Changes,
        JsonElement? Data,
        DateTimeOffset CreatedAt);
}
