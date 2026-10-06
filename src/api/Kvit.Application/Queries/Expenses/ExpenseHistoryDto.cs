using Kvit.Domain.Entities;

namespace Kvit.Application.Queries.Expenses
{
    public sealed record ExpenseHistoryDto(ActivityEventType Type, string ActorName, DateTimeOffset CreatedAt, string? Changes);
}
