using Kvit.Domain.Results;

namespace Kvit.Domain.Entities
{
    public class AdminAction
    {
        public const int ReasonMaxLength = 500;
        public const int TargetLabelMaxLength = 120;
        public const int LogDefaultLimit = 50;
        public const int LogMaxLimit = 200;

        private AdminAction()
        {
        }

        public long Id { get; private set; }

        public Guid AdminUserId { get; private set; }

        public AdminActionType Action { get; private set; }

        public Guid? TargetUserId { get; private set; }

        public Guid? TargetGroupId { get; private set; }

        public string TargetLabel { get; private set; } = string.Empty;

        public string Reason { get; private set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; private set; }

        public static Result<string> ReasonOf(string? reason)
        {
            string trimmedReason = reason?.Trim() ?? string.Empty;
            if (trimmedReason.Length == 0 || trimmedReason.Length > ReasonMaxLength)
            {
                return Result.Failure<string>(
                    $"A reason is needed: at least 1 and at most {ReasonMaxLength} characters after trimming spaces; it has {trimmedReason.Length}.",
                    ResultCodes.OWNER_REASON_INVALID);
            }

            return Result.Ok(trimmedReason);
        }

        public static int LogLimitOf(int? limit)
        {
            if (limit is not int requested || requested <= 0)
            {
                return LogDefaultLimit;
            }

            return Math.Min(requested, LogMaxLimit);
        }

        public static AdminAction AboutPerson(Guid adminUserId, AdminActionType action, Guid targetUserId, string targetLabel, string reason, DateTimeOffset createdAt)
        {
            return new AdminAction
            {
                AdminUserId = adminUserId,
                Action = action,
                TargetUserId = targetUserId,
                TargetLabel = targetLabel,
                Reason = reason,
                CreatedAt = createdAt,
            };
        }

        public static AdminAction AboutGroup(Guid adminUserId, AdminActionType action, Guid targetGroupId, string targetLabel, string reason, DateTimeOffset createdAt)
        {
            return new AdminAction
            {
                AdminUserId = adminUserId,
                Action = action,
                TargetGroupId = targetGroupId,
                TargetLabel = targetLabel,
                Reason = reason,
                CreatedAt = createdAt,
            };
        }
    }
}
