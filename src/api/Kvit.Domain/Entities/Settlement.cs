using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;
using Kvit.Domain.Settlements;

namespace Kvit.Domain.Entities
{
    public class Settlement
    {
        public const long AmountMaxMinorUnits = ExpenseFields.AmountMaxMinorUnits;

        private Settlement()
        {
        }

        public Guid Id { get; private set; }

        public Guid GroupId { get; private set; }

        public Guid FromMemberId { get; private set; }

        public Guid ToMemberId { get; private set; }

        public long AmountMinor { get; private set; }

        public Currency Currency { get; private set; }

        public SettlementStatus Status { get; private set; }

        public Guid RecordedByUserId { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public Guid? ResolvedByUserId { get; private set; }

        public DateTimeOffset? ResolvedAt { get; private set; }

        public DateTimeOffset? DeletedAt { get; private set; }

        public Guid? DeletedByUserId { get; private set; }

        public Guid ClientRequestId { get; private set; }

        public static Result<Settlement> Create(
            Guid groupId,
            SettlementInput input,
            GroupRoster roster,
            Guid recordedByUserId,
            Guid groupOwnerUserId,
            Guid clientRequestId,
            DateTimeOffset createdAt)
        {
            Result<Money> amount = AmountOf(input.AmountMinor, input.Currency);
            if (!amount.IsSuccess)
            {
                return amount.ToFailure<Settlement>();
            }

            if (input.FromMemberId == input.ToMemberId)
            {
                return Result.Failure<Settlement>($"A payment needs two different people; member {input.FromMemberId} is on both sides.", ResultCodes.SETTLEMENT_SAME_MEMBER);
            }

            Result<NamedMember> from = roster.FindCurrent(input.FromMemberId);
            if (!from.IsSuccess)
            {
                return MemberNotCurrent<Settlement>(input.FromMemberId);
            }

            Result<NamedMember> to = roster.FindCurrent(input.ToMemberId);
            if (!to.IsSuccess)
            {
                return MemberNotCurrent<Settlement>(input.ToMemberId);
            }

            Result<SettlementStatus> status = StatusWhenRecordedBy(from.Value.Member, to.Value.Member, recordedByUserId, groupOwnerUserId);
            if (!status.IsSuccess)
            {
                return status.ToFailure<Settlement>();
            }

            Settlement settlement = new()
            {
                Id = Guid.CreateVersion7(),
                GroupId = groupId,
                FromMemberId = input.FromMemberId,
                ToMemberId = input.ToMemberId,
                AmountMinor = amount.Value.MinorUnits,
                Currency = amount.Value.Currency,
                Status = SettlementStatus.Pending,
                RecordedByUserId = recordedByUserId,
                CreatedAt = createdAt,
                ClientRequestId = clientRequestId,
            };
            if (status.Value == SettlementStatus.Confirmed)
            {
                settlement.Resolve(SettlementStatus.Confirmed, recordedByUserId, createdAt);
            }

            return Result.Ok(settlement);
        }

        public static Result<T> NotFound<T>(Guid settlementId)
        {
            return Result.NotFound<T>($"Payment {settlementId} does not exist in this group, or it was deleted.", ResultCodes.SETTLEMENT_NOT_FOUND);
        }

        public static Result<T> ClientRequestIdUsed<T>(Guid clientRequestId)
        {
            return Result.Failure<T>($"The request id {clientRequestId} was already used for another group or by another person.", ResultCodes.SETTLEMENT_CLIENT_REQUEST_ID_USED);
        }

        public Result Confirm(GroupRoster roster, Guid byUserId, Guid groupOwnerUserId, DateTimeOffset confirmedAt)
        {
            Result allowed = CheckCanActFor(roster.RowOf(ToMemberId).Member, byUserId, groupOwnerUserId, "the receiver");
            if (!allowed.IsSuccess)
            {
                return allowed;
            }

            return Resolve(SettlementStatus.Confirmed, byUserId, confirmedAt);
        }

        public Result Reject(GroupRoster roster, Guid byUserId, Guid groupOwnerUserId, DateTimeOffset rejectedAt)
        {
            Result allowed = CheckCanActFor(roster.RowOf(ToMemberId).Member, byUserId, groupOwnerUserId, "the receiver");
            if (!allowed.IsSuccess)
            {
                return allowed;
            }

            return Resolve(SettlementStatus.Rejected, byUserId, rejectedAt);
        }

        public Result Cancel(GroupRoster roster, Guid byUserId, Guid groupOwnerUserId, DateTimeOffset cancelledAt)
        {
            Result allowed = CheckCanActFor(roster.RowOf(FromMemberId).Member, byUserId, groupOwnerUserId, "the payer");
            if (!allowed.IsSuccess)
            {
                return allowed;
            }

            return Resolve(SettlementStatus.Cancelled, byUserId, cancelledAt);
        }

        public Result Delete(GroupRoster roster, Guid byUserId, Guid groupOwnerUserId, DateTimeOffset deletedAt)
        {
            Result allowed = CheckRecorderOrOwner(byUserId, groupOwnerUserId);
            if (!allowed.IsSuccess)
            {
                return allowed;
            }

            Result bothCurrent = CheckBothSidesCurrent(roster);
            if (!bothCurrent.IsSuccess)
            {
                return bothCurrent;
            }

            if (Status != SettlementStatus.Confirmed)
            {
                return Result.Failure($"Payment {Id} is {Status}; only a confirmed payment can be deleted.", ResultCodes.SETTLEMENT_NOT_CONFIRMED);
            }

            DeletedAt = deletedAt;
            DeletedByUserId = byUserId;

            return Result.Ok();
        }

        public Result Restore(GroupRoster roster, Guid byUserId, Guid groupOwnerUserId)
        {
            Result allowed = CheckRecorderOrOwner(byUserId, groupOwnerUserId);
            if (!allowed.IsSuccess)
            {
                return allowed;
            }

            Result bothCurrent = CheckBothSidesCurrent(roster);
            if (!bothCurrent.IsSuccess)
            {
                return bothCurrent;
            }

            if (DeletedAt is null)
            {
                return Result.Failure($"Payment {Id} is not deleted.", ResultCodes.SETTLEMENT_NOT_DELETED);
            }

            DeletedAt = null;
            DeletedByUserId = null;

            return Result.Ok();
        }

        private static Result<Money> AmountOf(long amountMinor, string currency)
        {
            string[] currencyNames = Enum.GetNames<Currency>();
            if (!currencyNames.Contains(currency, StringComparer.Ordinal))
            {
                return Result.Failure<Money>($"'{currency}' is not a supported currency. Use one of: {string.Join(", ", currencyNames)}.", ResultCodes.SETTLEMENT_CURRENCY_INVALID);
            }

            if (amountMinor <= 0)
            {
                return Result.Failure<Money>($"The payment amount must be more than 0; got {amountMinor} minor units.", ResultCodes.SETTLEMENT_AMOUNT_NOT_POSITIVE);
            }

            if (amountMinor > AmountMaxMinorUnits)
            {
                return Result.Failure<Money>($"The payment amount must be at most {AmountMaxMinorUnits} minor units; got {amountMinor}.", ResultCodes.SETTLEMENT_AMOUNT_TOO_LARGE);
            }

            return Money.Create(amountMinor, Enum.Parse<Currency>(currency));
        }

        private static Result<T> MemberNotCurrent<T>(Guid memberId)
        {
            return Result.Failure<T>($"Member {memberId} is not a current member of this group.", ResultCodes.SETTLEMENT_MEMBER_NOT_CURRENT);
        }

        private static Result<SettlementStatus> StatusWhenRecordedBy(GroupMember from, GroupMember to, Guid userId, Guid groupOwnerUserId)
        {
            if (to.UserId == userId)
            {
                return Result.Ok(SettlementStatus.Confirmed);
            }

            if (from.UserId == userId)
            {
                bool isOwnerPayingAPlainName = userId == groupOwnerUserId && to.UserId is null;
                return Result.Ok(isOwnerPayingAPlainName ? SettlementStatus.Confirmed : SettlementStatus.Pending);
            }

            if (userId == groupOwnerUserId && to.UserId is null)
            {
                return Result.Ok(SettlementStatus.Confirmed);
            }

            if (userId == groupOwnerUserId && from.UserId is null)
            {
                return Result.Ok(SettlementStatus.Pending);
            }

            return Result.Forbid<SettlementStatus>(
                $"Only the payer, the receiver, or the group's owner acting for a name without an account can record a payment from member {from.Id} to member {to.Id}.",
                ResultCodes.SETTLEMENT_NOT_ALLOWED);
        }

        private Result CheckCanActFor(GroupMember side, Guid userId, Guid groupOwnerUserId, string sideName)
        {
            bool isOwnRow = side.UserId == userId;
            bool isOwnerActingForPlainName = side.UserId is null && userId == groupOwnerUserId;
            if (!isOwnRow && !isOwnerActingForPlainName)
            {
                return Result.Forbid($"Only {sideName} of payment {Id}, or the group's owner when {sideName} has no account, can do this.", ResultCodes.SETTLEMENT_NOT_ALLOWED);
            }

            return Result.Ok();
        }

        private Result CheckRecorderOrOwner(Guid userId, Guid groupOwnerUserId)
        {
            if (RecordedByUserId != userId && groupOwnerUserId != userId)
            {
                return Result.Forbid($"Only the person who recorded payment {Id} or the owner of its group can do this.", ResultCodes.SETTLEMENT_NOT_ALLOWED);
            }

            return Result.Ok();
        }

        private Result CheckBothSidesCurrent(GroupRoster roster)
        {
            if (!roster.FindCurrent(FromMemberId).IsSuccess)
            {
                return MemberNotCurrent<Settlement>(FromMemberId);
            }

            if (!roster.FindCurrent(ToMemberId).IsSuccess)
            {
                return MemberNotCurrent<Settlement>(ToMemberId);
            }

            return Result.Ok();
        }

        private Result Resolve(SettlementStatus status, Guid byUserId, DateTimeOffset resolvedAt)
        {
            if (Status != SettlementStatus.Pending)
            {
                return Result.Failure($"Payment {Id} is {Status}; only a pending payment can be confirmed, rejected or cancelled.", ResultCodes.SETTLEMENT_NOT_PENDING);
            }

            Status = status;
            ResolvedByUserId = byUserId;
            ResolvedAt = resolvedAt;

            return Result.Ok();
        }
    }
}
