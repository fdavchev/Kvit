using Kvit.Domain.Entities;

namespace Kvit.Contracts.Me
{
    public sealed record ExportedDuplicateAnswer(Guid PersonalEntryId, Guid ExpenseId, DuplicateAnswerChoice Answer, DateTimeOffset CreatedAt);
}
