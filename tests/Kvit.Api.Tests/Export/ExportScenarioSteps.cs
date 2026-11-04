using System.Net;
using Kvit.Api.Tests.BudgetCurrency;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalCategories;
using Kvit.Api.Tests.PersonalEntries;
using Kvit.Api.Tests.RecurringIncomes;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Export
{
    public static class ExportScenarioSteps
    {
        public const string OtherDisplayName = "Bojan Petrov";
        public const string OtherPictureUrl = "https://lh3.googleusercontent.com/a/bojan-private-picture";
        public const string PlainName = "Marko";
        public const string RemovedPlainName = "Zoran";
        public const string DeletedGroupName = "Deleted trip";
        public const string DeletedGroupExpenseTitle = "Hidden in a deleted group";
        public const string LeftGroupName = "Left trip";
        public const string StrangersGroupName = "Strangers trip";
        public const string DinnerTitle = "Dinner";
        public const string DinnerNote = "Taverna by the lake";
        public const string TaxiTitle = "Taxi";
        public const string DeletedExpenseTitle = "Deleted dinner";
        public const string CoffeeCategoryName = "Coffee beans";
        public const string ArchivedCategoryName = "Old hobby";
        public const string CoffeeNote = "Coffee for the office";
        public const string BonusNote = "Bonus note";
        public const string DeletedEntryNote = "exporter-deleted-note";
        public const string SalaryTitle = "Salary";
        public const string StoppedIncomeTitle = "Old side job";
        public const string OtherCategoryName = "Other secret category";
        public const string OtherEntryNote = "other-secret-note";
        public const string OtherIncomeNote = "other-secret-income";
        public const string OtherSalaryTitle = "Other secret wage";
        public const decimal MkdPerEur = 61.5610m;

        public static readonly DateOnly GeneratedIncomeDate = new(2026, 10, 1);
        public static readonly DateOnly FirstLimitMonth = new(2026, 10, 1);
        public static readonly DateOnly SecondLimitMonth = new(2026, 11, 1);

        public static async Task<ExportScenario> CreateAsync(ExpensesApp app)
        {
            app.Clock.SetTo(ExpensesApp.ClockStart);
            ExpenseGroup group = await app.CreateExpenseGroupAsync(PlainName);
            SignedInUser exporter = group.Owner;
            SignedInUser other = group.Member;
            await UserRows.SetDisplayNameAsync(app, other.UserId, OtherDisplayName);
            await UserRows.SetGooglePictureUrlAsync(app, other.UserId, OtherPictureUrl);
            await ExportRows.SetCountsInBudgetAsync(app, group.OwnerRowId, false);
            string previousInviteToken = $"previous-token-{Guid.NewGuid():N}";
            await GroupRows.SetPreviousInviteTokenAsync(app, group.GroupId, previousInviteToken);
            Guid removedPlainRowId = await GroupRows.InsertMemberAsync(
                app,
                group.GroupId,
                null,
                RemovedPlainName,
                exporter.UserId,
                removedAt: app.Clock.GetUtcNow(),
                endKind: "Removed",
                joinedAt: app.Clock.GetUtcNow() + (ExpensesApp.JoiningGap * 5));

            await BudgetCurrencySteps.UseRateAsync(app, MkdPerEur);
            Guid dinnerExpenseId = await AddDinnerAsync(app, group);
            Guid taxiExpenseId = await AddTaxiAsync(app, group);
            Guid deletedExpenseId = await AddAndDeleteExpenseAsync(app, group);

            Guid confirmedPaymentId = await SettlementSteps.RecordAsync(other, group.GroupId, group.MemberRowId, group.OwnerRowId, 2_500, ExpenseInputs.Eur);
            await SettlementSteps.ConfirmAsync(exporter, group.GroupId, confirmedPaymentId);
            Guid pendingPaymentId = await SettlementSteps.RecordAsync(other, group.GroupId, group.MemberRowId, group.OwnerRowId, 30_000, ExpenseInputs.Mkd);
            Guid deletedPaymentId = await SettlementSteps.RecordAsync(exporter, group.GroupId, group.MemberRowId, group.OwnerRowId, 11_100, ExpenseInputs.Mkd);
            await SettlementSteps.DeleteAsync(exporter, group.GroupId, deletedPaymentId);

            Guid deletedGroupId = await CreateDeletedGroupAsync(app, exporter);
            Guid leftGroupId = await CreateLeftGroupAsync(app, exporter, other);
            Guid strangersGroupId = await GroupRows.InsertGroupWithOwnerAsync(app, other.UserId, name: StrangersGroupName);

            Guid food = PersonalEntryRequests.BuiltInFoodId;
            Guid coffeeCategoryId = await PersonalCategorySteps.CreateAsync(exporter.Client, CoffeeCategoryName);
            Guid archivedCategoryId = await PersonalCategorySteps.CreateArchivedAsync(exporter.Client, ArchivedCategoryName);
            Guid coffeeEntryId = await PersonalEntrySteps.AddAsync(exporter.Client, EntryInput.Of(coffeeCategoryId, 2_500, ExpenseInputs.Eur, "2026-10-03", CoffeeNote));
            Guid foodEntryId = await PersonalEntrySteps.AddAsync(exporter.Client, EntryInput.Of(food, 150_000, ExpenseInputs.Mkd, "2026-10-04", null));
            Guid bonusEntryId = await PersonalEntrySteps.AddIncomeAsync(exporter.Client, IncomeEntryInput.Of(10_000, ExpenseInputs.Eur, "2026-10-02", BonusNote));
            Guid deletedEntryId = await PersonalEntrySteps.AddAndDeleteAsync(exporter.Client, EntryInput.Of(food, 9_900, ExpenseInputs.Mkd, "2026-10-04", DeletedEntryNote));
            Guid salaryId = await RecurringIncomeSteps.CreateAsync(exporter.Client, RecurringIncomeBody.Monthly(4_500_000, ExpenseInputs.Mkd, SalaryTitle, 15, RecurringIncomeBody.DefaultStart));
            Guid stoppedIncomeId = await RecurringIncomeSteps.CreateAndStopAsync(exporter.Client, RecurringIncomeBody.Weekly(120_000, ExpenseInputs.Mkd, StoppedIncomeTitle, 3, RecurringIncomeBody.DefaultStart));
            await RecurringIncomeRows.InsertEntryAsync(app, exporter.UserId, salaryId, GeneratedIncomeDate);
            Guid generatedEntryId = Guid.Parse(await ExportRows.RecurringIncomeEntryIdAsync(app, salaryId));
            await BudgetLimitRows.InsertAsync(app, exporter.UserId, null, FirstLimitMonth, 800_000);
            await BudgetLimitRows.InsertAsync(app, exporter.UserId, food, FirstLimitMonth, 300_000);
            await BudgetLimitRows.InsertAsync(app, exporter.UserId, food, SecondLimitMonth, null);
            await DuplicateAnswerRows.InsertAsync(app, exporter.UserId, foodEntryId, dinnerExpenseId, "Same");

            Guid otherCategoryId = await PersonalCategorySteps.CreateAsync(other.Client, OtherCategoryName);
            Guid otherEntryId = await PersonalEntrySteps.AddAsync(other.Client, EntryInput.Of(otherCategoryId, 7_700, ExpenseInputs.Eur, "2026-10-03", OtherEntryNote));
            Guid otherIncomeEntryId = await PersonalEntrySteps.AddIncomeAsync(other.Client, IncomeEntryInput.Of(55_500, ExpenseInputs.Mkd, "2026-10-02", OtherIncomeNote));
            Guid otherRecurringIncomeId = await RecurringIncomeSteps.CreateAsync(other.Client, RecurringIncomeBody.Monthly(3_300_000, ExpenseInputs.Mkd, OtherSalaryTitle, 10, RecurringIncomeBody.DefaultStart));
            await BudgetLimitRows.InsertAsync(app, other.UserId, otherCategoryId, FirstLimitMonth, 654_300);
            await DuplicateAnswerRows.InsertAsync(app, other.UserId, otherEntryId, dinnerExpenseId, "New");

            return new ExportScenario
            {
                Group = group,
                RemovedPlainRowId = removedPlainRowId,
                DinnerExpenseId = dinnerExpenseId,
                TaxiExpenseId = taxiExpenseId,
                DeletedExpenseId = deletedExpenseId,
                ConfirmedPaymentId = confirmedPaymentId,
                PendingPaymentId = pendingPaymentId,
                DeletedPaymentId = deletedPaymentId,
                DeletedGroupId = deletedGroupId,
                LeftGroupId = leftGroupId,
                StrangersGroupId = strangersGroupId,
                PreviousInviteToken = previousInviteToken,
                CoffeeCategoryId = coffeeCategoryId,
                ArchivedCategoryId = archivedCategoryId,
                CoffeeEntryId = coffeeEntryId,
                FoodEntryId = foodEntryId,
                BonusEntryId = bonusEntryId,
                DeletedEntryId = deletedEntryId,
                GeneratedEntryId = generatedEntryId,
                SalaryId = salaryId,
                StoppedIncomeId = stoppedIncomeId,
                OtherCategoryId = otherCategoryId,
                OtherEntryId = otherEntryId,
                OtherIncomeEntryId = otherIncomeEntryId,
                OtherRecurringIncomeId = otherRecurringIncomeId,
            };
        }

        private static Task<Guid> AddDinnerAsync(ExpensesApp app, ExpenseGroup group)
        {
            ExpenseInput dinner = ExpenseInputs.Equal(120_000, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId, group.PlainRowIds[0]) with
            {
                Title = DinnerTitle,
                Note = DinnerNote,
                CategoryId = PersonalEntryRequests.BuiltInFoodId,
            };

            return app.AddExpenseAsync(group.Owner, group.GroupId, dinner);
        }

        private static Task<Guid> AddTaxiAsync(ExpensesApp app, ExpenseGroup group)
        {
            ExpenseInput taxi = ExpenseInputs.Of(
                "Exact",
                3_000,
                ExpenseInputs.Eur,
                group.MemberRowId,
                new ShareInput(group.OwnerRowId, 1_000),
                new ShareInput(group.MemberRowId, 2_000)) with { Title = TaxiTitle };

            return app.AddExpenseAsync(group.Owner, group.GroupId, taxi);
        }

        private static async Task<Guid> AddAndDeleteExpenseAsync(ExpensesApp app, ExpenseGroup group)
        {
            ExpenseInput input = ExpenseInputs.Equal(5_000, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId) with { Title = DeletedExpenseTitle };
            Guid expenseId = await app.AddExpenseAsync(group.Owner, group.GroupId, input);

            HttpResponseMessage deleted = await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
            return expenseId;
        }

        private static async Task<Guid> CreateDeletedGroupAsync(ExpensesApp app, SignedInUser exporter)
        {
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(app, exporter.UserId, name: DeletedGroupName);
            Guid memberRowId = await GroupRows.CurrentMemberIdAsync(app, groupId, exporter.UserId);
            await ExpenseRows.InsertExpenseAsync(app, groupId, memberRowId, exporter.UserId, title: DeletedGroupExpenseTitle);
            await GroupRows.SetDeletedAsync(app, groupId, exporter.UserId, 1);

            return groupId;
        }

        private static async Task<Guid> CreateLeftGroupAsync(ExpensesApp app, SignedInUser exporter, SignedInUser other)
        {
            DateTimeOffset ownerJoinedAt = app.Clock.GetUtcNow() - (ExpensesApp.JoiningGap * 2);
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(app, other.UserId, name: LeftGroupName, ownerJoinedAt: ownerJoinedAt);
            await GroupRows.InsertMemberAsync(app, groupId, exporter.UserId, exporter.Form.DisplayName, other.UserId, joinedAt: ownerJoinedAt + ExpensesApp.JoiningGap);

            HttpResponseMessage left = await GroupRequests.LeaveAsync(exporter.Client, groupId);

            Assert.Equal(HttpStatusCode.NoContent, left.StatusCode);
            return groupId;
        }
    }
}
