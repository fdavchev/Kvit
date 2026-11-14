using System.Text.Json;
using Kvit.Domain.Entities;

namespace Kvit.Contracts.Me
{
    public sealed record ExportedActivityEvent(
        Guid Id,
        ActivityEventType Type,
        string ActorName,
        Guid? ExpenseId,
        Guid? MemberId,
        JsonElement? Changes,
        JsonElement? Data,
        DateTimeOffset CreatedAt);
}
