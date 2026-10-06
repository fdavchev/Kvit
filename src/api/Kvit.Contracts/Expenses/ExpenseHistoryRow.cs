using System.Text.Json;
using Kvit.Domain.Entities;

namespace Kvit.Contracts.Expenses
{
    public sealed record ExpenseHistoryRow(ActivityEventType Type, string ActorName, DateTimeOffset CreatedAt, JsonElement? Changes);
}
