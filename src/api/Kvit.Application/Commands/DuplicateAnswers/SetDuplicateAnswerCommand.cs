namespace Kvit.Application.Commands.DuplicateAnswers
{
    public sealed record SetDuplicateAnswerCommand(Guid EntryId, Guid ExpenseId, string? Answer);
}
