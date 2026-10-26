using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.BudgetCurrency;
using Kvit.Api.Tests.BudgetLimits;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.PersonalCategories;
using Kvit.Api.Tests.PersonalEntries;
using Xunit;

namespace Kvit.Api.Tests.BudgetSummary
{
    public class SummaryTotalsTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        [Fact]
        public async Task Summary_TwoMonthsInBothCurrencies_ListsMkdThenEur()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetSummarySteps.AddTwoMonthsAsync(_app, group);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(group.Member.Client);

            Assert.Equal([BudgetCurrencyRequests.Mkd, BudgetCurrencyRequests.Eur], BudgetSummarySteps.CurrencyNamesOf(summary));
        }

        [Fact]
        public async Task Summary_EurAddedBeforeMkd_StillListsMkdFirst()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Eur, 61.0000m);
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 5_000, BudgetCurrencyRequests.Mkd, 61.0000m);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(user.Client);

            Assert.Equal([BudgetCurrencyRequests.Mkd, BudgetCurrencyRequests.Eur], BudgetSummarySteps.CurrencyNamesOf(summary));
        }

        [Fact]
        public async Task Summary_MkdLine_HasPersonalSpendingPlusGroupSharesAndTheMkdIncomeOfBothMonths()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetSummarySteps.AddTwoMonthsAsync(_app, group);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(group.Member.Client);

            BudgetSummarySteps.AssertCurrencyRow(
                summary,
                BudgetCurrencyRequests.Mkd,
                BudgetSummarySteps.MkdSpendingThis,
                BudgetSummarySteps.MkdSpendingPrevious,
                BudgetSummarySteps.MkdIncomeThis,
                BudgetSummarySteps.MkdIncomePrevious);
        }

        [Fact]
        public async Task Summary_EurLine_HasPersonalSpendingPlusGroupSharesAndTheEurIncomeOfBothMonths()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetSummarySteps.AddTwoMonthsAsync(_app, group);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(group.Member.Client);

            BudgetSummarySteps.AssertCurrencyRow(
                summary,
                BudgetCurrencyRequests.Eur,
                BudgetSummarySteps.EurSpendingThis,
                BudgetSummarySteps.EurSpendingPrevious,
                BudgetSummarySteps.EurIncomeThis,
                BudgetSummarySteps.EurIncomePrevious);
        }

        [Fact]
        public async Task Summary_MkdCategories_HaveBothMonthsAndLeaveOutTheGroupExpenseWithoutACategory()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetSummarySteps.AddTwoMonthsAsync(_app, group);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(group.Member.Client);

            BudgetSummarySteps.AssertCategories(
                summary,
                BudgetCurrencyRequests.Mkd,
                (BudgetLimitSteps.Food, 150_000, 70_000),
                (BudgetLimitSteps.Transport, 0, 30_000));
        }

        [Fact]
        public async Task Summary_EurCategories_HaveBothMonths()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetSummarySteps.AddTwoMonthsAsync(_app, group);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(group.Member.Client);

            BudgetSummarySteps.AssertCategories(
                summary,
                BudgetCurrencyRequests.Eur,
                (BudgetLimitSteps.Food, 2_500, 1_234),
                (BudgetLimitSteps.Transport, 1_550, 1_000));
        }

        [Fact]
        public async Task Summary_ViewingTheMonthBefore_SwapsWhatIsThisAndWhatIsPrevious()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetSummarySteps.AddTwoMonthsAsync(_app, group);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(group.Member.Client, BudgetSummarySteps.PreviousMonth);

            Assert.Equal(BudgetSummarySteps.PreviousMonth, summary.GetProperty("month").GetString());
            Assert.Equal("2026-08", summary.GetProperty("previousMonth").GetString());
            BudgetSummarySteps.AssertCurrencyRow(summary, BudgetCurrencyRequests.Mkd, BudgetSummarySteps.MkdSpendingPrevious, 0, BudgetSummarySteps.MkdIncomePrevious, 0);
            BudgetSummarySteps.AssertCategories(
                summary,
                BudgetCurrencyRequests.Mkd,
                (BudgetLimitSteps.Food, 70_000, 0),
                (BudgetLimitSteps.Transport, 30_000, 0));
        }

        [Fact]
        public async Task Summary_AgreesWithTheMonthAnswersOfBothMonths()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetSummarySteps.AddTwoMonthsAsync(_app, group);
            HttpClient client = group.Member.Client;

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(client);
            JsonElement thisMonth = await PersonalEntrySteps.MonthAsync(client, BudgetSummarySteps.ThisMonth);
            JsonElement previousMonth = await PersonalEntrySteps.MonthAsync(client, BudgetSummarySteps.PreviousMonth);

            foreach (string currency in new[] { BudgetCurrencyRequests.Mkd, BudgetCurrencyRequests.Eur })
            {
                JsonElement row = BudgetSummarySteps.CurrencyRowOf(summary, currency);
                Assert.Equal(SpendingOf(thisMonth, currency), row.GetProperty("spendingThisMinor").GetInt64());
                Assert.Equal(SpendingOf(previousMonth, currency), row.GetProperty("spendingPreviousMinor").GetInt64());
                Assert.Equal(IncomeOf(thisMonth, currency), row.GetProperty("incomeThisMinor").GetInt64());
                Assert.Equal(IncomeOf(previousMonth, currency), row.GetProperty("incomePreviousMinor").GetInt64());
            }
        }

        [Fact]
        public async Task Summary_CurrencyWithSpendingOnlyInThePreviousMonth_IsListedWithZeroForThisMonth()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 4_000, BudgetCurrencyRequests.Eur, 61.0000m, "2026-09-10");

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(user.Client);

            Assert.Equal([BudgetCurrencyRequests.Eur], BudgetSummarySteps.CurrencyNamesOf(summary));
            BudgetSummarySteps.AssertCurrencyRow(summary, BudgetCurrencyRequests.Eur, 0, 4_000, 0, 0);
            BudgetSummarySteps.AssertCategories(summary, BudgetCurrencyRequests.Eur, (BudgetLimitSteps.Food, 0, 4_000));
        }

        [Fact]
        public async Task Summary_CurrencyWithSpendingOnlyInThisMonth_IsListedWithZeroForThePreviousMonth()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 4_000, BudgetCurrencyRequests.Mkd, 61.0000m);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(user.Client);

            Assert.Equal([BudgetCurrencyRequests.Mkd], BudgetSummarySteps.CurrencyNamesOf(summary));
            BudgetSummarySteps.AssertCurrencyRow(summary, BudgetCurrencyRequests.Mkd, 4_000, 0, 0, 0);
            BudgetSummarySteps.AssertCategories(summary, BudgetCurrencyRequests.Mkd, (BudgetLimitSteps.Food, 4_000, 0));
        }

        [Fact]
        public async Task Summary_CurrencyWithOnlyIncome_IsListedWithZeroSpendingAndNoCategories()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetCurrencySteps.AddIncomeAsync(_app, user, 4_500_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await BudgetCurrencySteps.AddIncomeAsync(_app, user, 4_000_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-09-25");

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(user.Client);

            Assert.Equal([BudgetCurrencyRequests.Mkd], BudgetSummarySteps.CurrencyNamesOf(summary));
            BudgetSummarySteps.AssertCurrencyRow(summary, BudgetCurrencyRequests.Mkd, 0, 0, 4_500_000, 4_000_000);
            BudgetSummarySteps.AssertCategories(summary, BudgetCurrencyRequests.Mkd);
        }

        [Fact]
        public async Task Summary_OnlyEurItems_HaveNoMkdLine()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 4_000, BudgetCurrencyRequests.Eur, 61.0000m);
            await BudgetCurrencySteps.AddIncomeAsync(_app, user, 9_000, BudgetCurrencyRequests.Eur, 61.0000m);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(user.Client);

            Assert.Equal([BudgetCurrencyRequests.Eur], BudgetSummarySteps.CurrencyNamesOf(summary));
        }

        [Fact]
        public async Task Summary_ItemsFromOtherMonthsThanTheTwoCompared_AreNotCounted()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-08-31");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 2_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-09-01");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 4_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-09-30");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 8_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-10-01");

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(user.Client);

            BudgetSummarySteps.AssertCurrencyRow(summary, BudgetCurrencyRequests.Mkd, 8_000, 6_000, 0, 0);
        }

        [Fact]
        public async Task Summary_CustomCategoryOnlyInThePreviousMonth_HasZeroForThisMonth()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateAsync(user.Client, "Pets");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, pets, 5_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-09-10");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Mkd, 61.0000m);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(user.Client);

            BudgetSummarySteps.AssertCategories(summary, BudgetCurrencyRequests.Mkd, (BudgetLimitSteps.Food, 1_000, 0), (pets, 0, 5_000));
        }

        [Fact]
        public async Task Summary_CustomCategoryOnlyInThisMonth_HasZeroForThePreviousMonth()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid gym = await PersonalCategorySteps.CreateAsync(user.Client, "Gym");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, gym, 3_000, BudgetCurrencyRequests.Mkd, 61.0000m);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(user.Client);

            BudgetSummarySteps.AssertCategories(summary, BudgetCurrencyRequests.Mkd, (gym, 3_000, 0));
        }

        [Fact]
        public async Task Summary_ArchivedCategory_IsStillComparedInBothMonths()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateAsync(user.Client, "Pets");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, pets, 5_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-09-10");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, pets, 8_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await PersonalCategoryRequests.ArchiveAsync(user.Client, pets);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(user.Client);

            BudgetSummarySteps.AssertCategories(summary, BudgetCurrencyRequests.Mkd, (pets, 8_000, 5_000));
        }

        [Fact]
        public async Task Summary_CategoryRows_AreBuiltInsInTheirOrderThenCustomInTheirOrderWhateverOrderTheSpendingWasAdded()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid gym = await PersonalCategorySteps.CreateAsync(user.Client, "Gym");
            Guid pets = await PersonalCategorySteps.CreateAsync(user.Client, "Pets");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, pets, 1_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Transport, 2_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-09-10");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, gym, 3_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-09-10");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 4_000, BudgetCurrencyRequests.Mkd, 61.0000m);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(user.Client);

            BudgetSummarySteps.AssertCategories(
                summary,
                BudgetCurrencyRequests.Mkd,
                (BudgetLimitSteps.Food, 4_000, 0),
                (BudgetLimitSteps.Transport, 0, 2_000),
                (gym, 0, 3_000),
                (pets, 1_000, 0));
        }

        [Fact]
        public async Task Summary_DeletedEntries_AreLeftOutAndRestoredEntriesAreBack()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            Guid deletedSpending = await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 9_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-09-10");
            Guid deletedIncome = await BudgetCurrencySteps.AddIncomeAsync(_app, user, 7_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await PersonalEntryRequests.DeleteAsync(user.Client, deletedSpending);
            await PersonalEntryRequests.DeleteAsync(user.Client, deletedIncome);

            JsonElement whileDeleted = await BudgetSummarySteps.SummaryAsync(user.Client);
            await PersonalEntryRequests.RestoreAsync(user.Client, deletedSpending);
            await PersonalEntryRequests.RestoreAsync(user.Client, deletedIncome);
            JsonElement afterRestore = await BudgetSummarySteps.SummaryAsync(user.Client);

            BudgetSummarySteps.AssertCurrencyRow(whileDeleted, BudgetCurrencyRequests.Mkd, 1_000, 0, 0, 0);
            BudgetSummarySteps.AssertCategories(whileDeleted, BudgetCurrencyRequests.Mkd, (BudgetLimitSteps.Food, 1_000, 0));
            BudgetSummarySteps.AssertCurrencyRow(afterRestore, BudgetCurrencyRequests.Mkd, 1_000, 9_000, 7_000, 0);
            BudgetSummarySteps.AssertCategories(afterRestore, BudgetCurrencyRequests.Mkd, (BudgetLimitSteps.Food, 1_000, 9_000));
        }

        [Fact]
        public async Task Summary_CategoryWhoseOnlySpendingWasDeleted_HasNoRow()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            Guid transport = await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Transport, 9_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await PersonalEntryRequests.DeleteAsync(user.Client, transport);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(user.Client);

            BudgetSummarySteps.AssertCategories(summary, BudgetCurrencyRequests.Mkd, (BudgetLimitSteps.Food, 1_000, 0));
        }

        [Fact]
        public async Task Summary_EverythingDeleted_HasAnEmptyCurrenciesList()
        {
            SignedInUser user = await _app.SignUpAsync();
            await PersonalEntrySteps.AddAndDeleteAsync(user.Client, EntryInput.Of(BudgetLimitSteps.Food, 1_000));
            await PersonalEntrySteps.AddIncomeAndDeleteAsync(user.Client, IncomeEntryInput.Of(9_000));

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(user.Client);

            BudgetSummarySteps.AssertNothingToCompare(summary);
        }

        [Fact]
        public async Task Summary_AnotherPersonsEntriesAndIncome_AreNeverCounted()
        {
            SignedInUser me = await _app.SignUpAsync();
            SignedInUser stranger = await _app.SignUpAsync("Bojan");
            await BudgetLimitSteps.AddSpendingAsync(_app, me, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await BudgetLimitSteps.AddSpendingAsync(_app, stranger, BudgetLimitSteps.Food, 90_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await BudgetLimitSteps.AddSpendingAsync(_app, stranger, BudgetLimitSteps.Transport, 5_000, BudgetCurrencyRequests.Eur, 61.0000m, "2026-09-10");
            await BudgetCurrencySteps.AddIncomeAsync(_app, stranger, 7_000, BudgetCurrencyRequests.Mkd, 61.0000m);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(me.Client);

            Assert.Equal([BudgetCurrencyRequests.Mkd], BudgetSummarySteps.CurrencyNamesOf(summary));
            BudgetSummarySteps.AssertCurrencyRow(summary, BudgetCurrencyRequests.Mkd, 1_000, 0, 0, 0);
            BudgetSummarySteps.AssertCategories(summary, BudgetCurrencyRequests.Mkd, (BudgetLimitSteps.Food, 1_000, 0));
        }

        [Fact]
        public async Task Summary_IncomeEntries_NeverBecomeCategoryRows()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetCurrencySteps.AddIncomeAsync(_app, user, 4_500_000, BudgetCurrencyRequests.Mkd, 61.0000m);
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Mkd, 61.0000m);

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(user.Client);

            BudgetSummarySteps.AssertCategories(summary, BudgetCurrencyRequests.Mkd, (BudgetLimitSteps.Food, 1_000, 0));
        }

        [Fact]
        public async Task Summary_FirstAndLastDaysOfJanuaryAndDecember_BelongToTheirOwnMonthOnly()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2025-11-30");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 2_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2025-12-01");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 4_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2025-12-31");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 8_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-01-01");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 16_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-01-31");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 32_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-02-01");

            JsonElement january = await BudgetSummarySteps.SummaryAsync(user.Client, "2026-01");
            JsonElement february = await BudgetSummarySteps.SummaryAsync(user.Client, "2026-02");

            BudgetSummarySteps.AssertCurrencyRow(january, BudgetCurrencyRequests.Mkd, 24_000, 6_000, 0, 0);
            BudgetSummarySteps.AssertCurrencyRow(february, BudgetCurrencyRequests.Mkd, 32_000, 24_000, 0, 0);
        }

        [Fact]
        public async Task Summary_IncomeOnTheFirstAndLastDaysOfJanuaryAndDecember_BelongsToTheirOwnMonthOnly()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetCurrencySteps.AddIncomeAsync(_app, user, 1_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2025-11-30");
            await BudgetCurrencySteps.AddIncomeAsync(_app, user, 2_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2025-12-01");
            await BudgetCurrencySteps.AddIncomeAsync(_app, user, 4_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2025-12-31");
            await BudgetCurrencySteps.AddIncomeAsync(_app, user, 8_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-01-01");
            await BudgetCurrencySteps.AddIncomeAsync(_app, user, 16_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-01-31");
            await BudgetCurrencySteps.AddIncomeAsync(_app, user, 32_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-02-01");

            JsonElement january = await BudgetSummarySteps.SummaryAsync(user.Client, "2026-01");

            BudgetSummarySteps.AssertCurrencyRow(january, BudgetCurrencyRequests.Mkd, 0, 0, 24_000, 6_000);
        }

        [Fact]
        public async Task Summary_MarchAgainstShortFebruary_SplitsOnTheLastDayOfFebruary()
        {
            SignedInUser user = await _app.SignUpAsync();
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 1_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2025-01-31");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 2_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2025-02-01");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 4_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2025-02-28");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 8_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2025-03-01");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 16_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2025-03-31");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, 32_000, BudgetCurrencyRequests.Mkd, 61.0000m, "2025-04-01");

            JsonElement summary = await BudgetSummarySteps.SummaryAsync(user.Client, "2025-03");

            Assert.Equal("2025-02", summary.GetProperty("previousMonth").GetString());
            BudgetSummarySteps.AssertCurrencyRow(summary, BudgetCurrencyRequests.Mkd, 24_000, 6_000, 0, 0);
        }

        [Fact]
        public async Task Summary_ReadingIt_ChangesNothingInTheMonthAnswer()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            await BudgetSummarySteps.AddTwoMonthsAsync(_app, group);
            JsonElement before = await PersonalEntrySteps.MonthAsync(group.Member.Client);

            await BudgetSummarySteps.SummaryAsync(group.Member.Client);
            JsonElement after = await PersonalEntrySteps.MonthAsync(group.Member.Client);

            Assert.Equal(before.GetRawText(), after.GetRawText());
        }

        private static long SpendingOf(JsonElement month, string currency)
        {
            long personal = month.GetProperty("totals").EnumerateArray()
                .Where(total => total.GetProperty("currency").GetString() == currency)
                .Sum(total => total.GetProperty("totalMinor").GetInt64());
            long shares = month.GetProperty("groupShares").EnumerateArray()
                .Where(share => share.GetProperty("currency").GetString() == currency)
                .Sum(share => share.GetProperty("shareMinor").GetInt64());

            return personal + shares;
        }

        private static long IncomeOf(JsonElement month, string currency)
        {
            return month.GetProperty("incomeTotals").EnumerateArray()
                .Where(total => total.GetProperty("currency").GetString() == currency)
                .Sum(total => total.GetProperty(PersonalEntryRequests.IncomeTotalAmountProperty).GetInt64());
        }
    }
}
