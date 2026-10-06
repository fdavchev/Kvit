using Kvit.Domain.ExchangeRates;
using Kvit.Domain.MoneyRules;
using Kvit.Domain.Results;

namespace Kvit.Domain.Entities
{
    public class Expense
    {
        public const int DeletedExpenseRestoreDays = 5;

        private readonly List<ExpenseShare> _shares = [];

        private Expense()
        {
        }

        public Guid Id { get; private set; }

        public Guid GroupId { get; private set; }

        public string? Title { get; private set; }

        public string? Note { get; private set; }

        public long AmountMinor { get; private set; }

        public Currency Currency { get; private set; }

        public DateOnly ExpenseDate { get; private set; }

        public Guid? CategoryId { get; private set; }

        public Guid PaidByMemberId { get; private set; }

        public SplitType SplitType { get; private set; }

        public decimal MkdPerEur { get; private set; }

        public DateOnly RateDate { get; private set; }

        public Guid CreatedByUserId { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public Guid? UpdatedByUserId { get; private set; }

        public DateTimeOffset? UpdatedAt { get; private set; }

        public DateTimeOffset? DeletedAt { get; private set; }

        public Guid? DeletedByUserId { get; private set; }

        public Guid ClientRequestId { get; private set; }

        public IReadOnlyList<ExpenseShare> Shares => _shares;

        public static Expense Create(
            Guid groupId,
            ExpenseFields fields,
            Guid paidByMemberId,
            IReadOnlyList<SplitShare> shares,
            ExchangeRateSnapshot rate,
            Guid createdByUserId,
            Guid clientRequestId,
            DateTimeOffset createdAt)
        {
            Expense expense = new()
            {
                Id = Guid.CreateVersion7(),
                GroupId = groupId,
                CreatedByUserId = createdByUserId,
                CreatedAt = createdAt,
                ClientRequestId = clientRequestId,
            };
            expense.ApplyFields(fields, paidByMemberId);
            expense.SaveRate(rate);
            expense.ReplaceShares(shares);

            return expense;
        }

        public static Result<T> NotFound<T>(Guid expenseId)
        {
            return Result.NotFound<T>($"Expense {expenseId} does not exist in this group, or it was deleted.", ResultCodes.EXPENSE_NOT_FOUND);
        }

        public static Result<T> ClientRequestIdUsed<T>(Guid clientRequestId)
        {
            return Result.Failure<T>($"The request id {clientRequestId} was already used for another group or by another person.", ResultCodes.EXPENSE_CLIENT_REQUEST_ID_USED);
        }

        public static Result<IReadOnlyList<SplitShare>> SplitAmong(GroupRoster roster, ExpenseFields fields, Guid paidByMemberId, IReadOnlyList<SplitInput> inputs)
        {
            Result<NamedMember> payer = roster.FindCurrent(paidByMemberId);
            if (!payer.IsSuccess)
            {
                return payer.ToFailure<IReadOnlyList<SplitShare>>();
            }

            Result<IReadOnlyList<SplitInput>> inJoiningOrder = roster.CurrentInJoiningOrder(inputs);
            if (!inJoiningOrder.IsSuccess)
            {
                return inJoiningOrder.ToFailure<IReadOnlyList<SplitShare>>();
            }

            return Splitter.Calculate(fields.SplitType, fields.Amount, paidByMemberId, inJoiningOrder.Value);
        }

        public static DateTimeOffset RestorableUntil(DateTimeOffset deletedAt)
        {
            return deletedAt.AddDays(DeletedExpenseRestoreDays);
        }

        public Result CheckCanBeChangedBy(Guid userId, Guid groupOwnerUserId)
        {
            if (CreatedByUserId != userId && groupOwnerUserId != userId)
            {
                return Result.Forbid($"Only the person who added expense {Id} or the owner of its group can do this.", ResultCodes.EXPENSE_NOT_ALLOWED);
            }

            return Result.Ok();
        }

        public void Update(ExpenseFields fields, Guid paidByMemberId, IReadOnlyList<SplitShare> shares, Guid byUserId, DateTimeOffset updatedAt)
        {
            ApplyFields(fields, paidByMemberId);
            ReplaceShares(shares);
            UpdatedByUserId = byUserId;
            UpdatedAt = updatedAt;
        }

        public void SaveRate(ExchangeRateSnapshot rate)
        {
            MkdPerEur = rate.MkdPerEur;
            RateDate = rate.RateDate;
        }

        public void Delete(Guid byUserId, DateTimeOffset deletedAt)
        {
            DeletedAt = deletedAt;
            DeletedByUserId = byUserId;
        }

        public Result Restore(DateTimeOffset now)
        {
            if (DeletedAt is not DateTimeOffset deletedAt)
            {
                return Result.Failure($"Expense {Id} is not deleted.", ResultCodes.EXPENSE_NOT_DELETED);
            }

            if (now >= RestorableUntil(deletedAt))
            {
                return Result.Failure($"Expense {Id} was deleted more than {DeletedExpenseRestoreDays} days ago and can no longer be restored.", ResultCodes.EXPENSE_RESTORE_EXPIRED);
            }

            DeletedAt = null;
            DeletedByUserId = null;

            return Result.Ok();
        }

        private void ApplyFields(ExpenseFields fields, Guid paidByMemberId)
        {
            Title = fields.Title;
            Note = fields.Note;
            AmountMinor = fields.Amount.MinorUnits;
            Currency = fields.Amount.Currency;
            ExpenseDate = fields.ExpenseDate;
            CategoryId = fields.CategoryId;
            SplitType = fields.SplitType;
            PaidByMemberId = paidByMemberId;
        }

        private void ReplaceShares(IReadOnlyList<SplitShare> shares)
        {
            _shares.RemoveAll(existing => !shares.Any(share => share.MemberId == existing.MemberId));
            foreach (SplitShare share in shares)
            {
                ExpenseShare? existing = _shares.SingleOrDefault(candidate => candidate.MemberId == share.MemberId);
                if (existing is null)
                {
                    _shares.Add(ExpenseShare.Create(Id, share));
                }
                else
                {
                    existing.Change(share);
                }
            }
        }
    }
}
