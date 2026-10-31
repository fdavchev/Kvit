namespace Kvit.Contracts.DuplicateAnswers
{
    public sealed record RemoveDuplicateAnswerRequest(Guid EntryId, Guid ExpenseId);
}
