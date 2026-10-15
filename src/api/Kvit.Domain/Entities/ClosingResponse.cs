using Kvit.Domain.Results;

namespace Kvit.Domain.Entities
{
    public class ClosingResponse
    {
        public const int ReasonMaxLength = 200;

        private ClosingResponse()
        {
        }

        public Guid GroupId { get; private set; }

        public Guid MemberId { get; private set; }

        public ClosingAnswer Response { get; private set; }

        public string? Reason { get; private set; }

        public DateTimeOffset RespondedAt { get; private set; }

        public static ClosingResponse Confirmed(Guid groupId, Guid memberId, DateTimeOffset respondedAt)
        {
            return new ClosingResponse
            {
                GroupId = groupId,
                MemberId = memberId,
                Response = ClosingAnswer.Confirmed,
                RespondedAt = respondedAt,
            };
        }

        public static Result<string?> ObjectionReasonOf(string? reason)
        {
            string? trimmedReason = ExpenseFields.TrimToNull(reason);
            if (trimmedReason is not null && trimmedReason.Length > ReasonMaxLength)
            {
                return Result.Failure<string?>($"The reason must have at most {ReasonMaxLength} characters; it has {trimmedReason.Length}.", ResultCodes.CLOSING_REASON_TOO_LONG);
            }

            return Result.Ok(trimmedReason);
        }
    }
}
