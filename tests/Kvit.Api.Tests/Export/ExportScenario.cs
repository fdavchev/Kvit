using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;

namespace Kvit.Api.Tests.Export
{
    public sealed class ExportScenario
    {
        public required ExpenseGroup Group { get; init; }

        public required Guid RemovedPlainRowId { get; init; }

        public required Guid DinnerExpenseId { get; init; }

        public required Guid TaxiExpenseId { get; init; }

        public required Guid DeletedExpenseId { get; init; }

        public required Guid ConfirmedPaymentId { get; init; }

        public required Guid PendingPaymentId { get; init; }

        public required Guid DeletedPaymentId { get; init; }

        public required Guid DeletedGroupId { get; init; }

        public required Guid DeletedGroupEventId { get; init; }

        public required Guid LeftGroupId { get; init; }

        public required Guid StrangersGroupId { get; init; }

        public required Guid StrangersEventId { get; init; }

        public required string PreviousInviteToken { get; init; }

        public required Guid CoffeeCategoryId { get; init; }

        public required Guid ArchivedCategoryId { get; init; }

        public required Guid CoffeeEntryId { get; init; }

        public required Guid FoodEntryId { get; init; }

        public required Guid BonusEntryId { get; init; }

        public required Guid DeletedEntryId { get; init; }

        public required Guid GeneratedEntryId { get; init; }

        public required Guid SalaryId { get; init; }

        public required Guid StoppedIncomeId { get; init; }

        public required Guid OtherCategoryId { get; init; }

        public required Guid OtherEntryId { get; init; }

        public required Guid OtherIncomeEntryId { get; init; }

        public required Guid OtherRecurringIncomeId { get; init; }

        public SignedInUser Exporter => Group.Owner;

        public SignedInUser Other => Group.Member;

        public Guid GroupId => Group.GroupId;

        public Guid PlainRowId => Group.PlainRowIds[0];
    }
}
