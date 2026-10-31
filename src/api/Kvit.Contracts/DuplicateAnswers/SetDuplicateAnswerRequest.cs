using System.Text.Json;

namespace Kvit.Contracts.DuplicateAnswers
{
    public sealed record SetDuplicateAnswerRequest(Guid EntryId, Guid ExpenseId, JsonElement Answer)
    {
        public string? AnswerAsText()
        {
            if (Answer.ValueKind == JsonValueKind.String)
            {
                return Answer.GetString();
            }

            return null;
        }
    }
}
