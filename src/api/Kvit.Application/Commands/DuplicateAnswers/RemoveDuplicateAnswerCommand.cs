namespace Kvit.Application.Commands.DuplicateAnswers
{
    public sealed record RemoveDuplicateAnswerCommand(Guid EntryId, Guid ExpenseId);
}
