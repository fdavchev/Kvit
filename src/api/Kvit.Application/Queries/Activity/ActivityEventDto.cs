using Kvit.Domain.Entities;

namespace Kvit.Application.Queries.Activity
{
    public sealed record ActivityEventDto(
        Guid Id,
        ActivityEventType Type,
        Guid ActorUserId,
        string ActorName,
        Guid? ExpenseId,
        Guid? MemberId,
        string? Changes,
        string? Data,
        DateTimeOffset CreatedAt);
}
