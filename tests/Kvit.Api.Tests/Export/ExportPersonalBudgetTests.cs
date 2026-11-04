using System.Globalization;
using System.Text.Json;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.PersonalCategories;
using Kvit.Api.Tests.PersonalEntries;
using Xunit;

namespace Kvit.Api.Tests.Export
{
    public class ExportPersonalBudgetTests(ExportApp _app) : IClassFixture<ExportApp>
    {
        [Fact]
        public async Task Export_Categories_AreOnlyTheExportersOwnCustomOnesIncludingTheArchivedOne()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            List<JsonElement> categories = ExportBody.ListOf(body.PersonalBudget, "categories");

            Assert.Equal(
                new[] { scenario.CoffeeCategoryId, scenario.ArchivedCategoryId }.Order(),
                ExportBody.IdsOf(categories).Order());
        }

        [Fact]
        public async Task Export_ActiveCategory_ShowsNameEmojiColorSortOrderAndNullArchivedAt()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement category = ExportBody.WithId(ExportBody.ListOf(body.PersonalBudget, "categories"), scenario.CoffeeCategoryId);

            Assert.Equal(ExportScenarioSteps.CoffeeCategoryName, category.GetProperty("name").GetString());
            Assert.Equal(PersonalCategoryRequests.DefaultEmoji, category.GetProperty("emoji").GetString());
            Assert.Equal(PersonalCategoryRequests.DefaultColor, category.GetProperty("color").GetString());
            Assert.Equal(JsonValueKind.Number, category.GetProperty("sortOrder").ValueKind);
            Assert.Null(ExportBody.StringOrNull(category, "archivedAt"));
        }

        [Fact]
        public async Task Export_ArchivedCategory_ShowsArchivedAt()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement category = ExportBody.WithId(ExportBody.ListOf(body.PersonalBudget, "categories"), scenario.ArchivedCategoryId);

            Assert.Equal(ExportScenarioSteps.ArchivedCategoryName, category.GetProperty("name").GetString());
            Assert.NotNull(ExportBody.StringOrNull(category, "archivedAt"));
        }

        [Fact]
        public async Task Export_Entries_AreTheLiveSpendingAndIncomeEntriesAndNotTheDeletedOne()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            List<JsonElement> entries = ExportBody.ListOf(body.PersonalBudget, "entries");

            Guid[] expected = [scenario.CoffeeEntryId, scenario.FoodEntryId, scenario.BonusEntryId, scenario.GeneratedEntryId];
            Assert.Equal(expected.Order(), ExportBody.IdsOf(entries).Order());
        }

        [Fact]
        public async Task Export_DeletedEntry_IsNotMentionedAnywhere()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            body.AssertDoesNotMention(scenario.DeletedEntryId.ToString());
            body.AssertDoesNotMention(ExportScenarioSteps.DeletedEntryNote);
        }

        [Fact]
        public async Task Export_SpendingInACustomCategory_ShowsAmountCurrencyDateNoteAndCategory()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement entry = EntryOf(body, scenario.CoffeeEntryId);

            Assert.Equal("Spending", entry.GetProperty("kind").GetString());
            Assert.Equal(2_500, entry.GetProperty("amountMinor").GetInt64());
            Assert.Equal("EUR", entry.GetProperty("currency").GetString());
            Assert.Equal("2026-10-03", entry.GetProperty("date").GetString());
            Assert.Equal(ExportScenarioSteps.CoffeeNote, entry.GetProperty("note").GetString());
            Assert.Equal(scenario.CoffeeCategoryId, entry.GetProperty("categoryId").GetGuid());
            Assert.Equal(ExportScenarioSteps.CoffeeCategoryName, entry.GetProperty("category").GetString());
            Assert.Null(ExportBody.GuidOrNull(entry, "recurringIncomeId"));
        }

        [Fact]
        public async Task Export_SpendingInABuiltInCategory_ShowsTheBuiltInKeyAndNullNote()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement entry = EntryOf(body, scenario.FoodEntryId);

            Assert.Equal("Spending", entry.GetProperty("kind").GetString());
            Assert.Equal(150_000, entry.GetProperty("amountMinor").GetInt64());
            Assert.Equal("MKD", entry.GetProperty("currency").GetString());
            Assert.Equal("2026-10-04", entry.GetProperty("date").GetString());
            Assert.Equal(PersonalEntryRequests.BuiltInFoodId, entry.GetProperty("categoryId").GetGuid());
            Assert.Equal("food", entry.GetProperty("category").GetString());
            Assert.Null(ExportBody.StringOrNull(entry, "note"));
        }

        [Fact]
        public async Task Export_IncomeEntry_ShowsKindIncomeWithNoCategory()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement entry = EntryOf(body, scenario.BonusEntryId);

            Assert.Equal("Income", entry.GetProperty("kind").GetString());
            Assert.Equal(10_000, entry.GetProperty("amountMinor").GetInt64());
            Assert.Equal("EUR", entry.GetProperty("currency").GetString());
            Assert.Equal("2026-10-02", entry.GetProperty("date").GetString());
            Assert.Equal(ExportScenarioSteps.BonusNote, entry.GetProperty("note").GetString());
            Assert.Null(ExportBody.GuidOrNull(entry, "categoryId"));
            Assert.Null(ExportBody.StringOrNull(entry, "category"));
        }

        [Fact]
        public async Task Export_IncomeMadeByARecurringIncome_ShowsTheLinkToThatRule()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement entry = EntryOf(body, scenario.GeneratedEntryId);

            Assert.Equal("Income", entry.GetProperty("kind").GetString());
            Assert.Equal(4_500_000, entry.GetProperty("amountMinor").GetInt64());
            Assert.Equal("2026-10-01", entry.GetProperty("date").GetString());
            Assert.Equal(scenario.SalaryId, entry.GetProperty("recurringIncomeId").GetGuid());
        }

        [Fact]
        public async Task Export_Entry_ShowsTheExchangeRateSavedWithIt()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement entry = EntryOf(body, scenario.CoffeeEntryId);

            string storedRate = await PersonalEntrySteps.ValueAsync(_app, scenario.CoffeeEntryId, "mkd_per_eur") ?? string.Empty;
            Assert.Equal(decimal.Parse(storedRate, CultureInfo.InvariantCulture), entry.GetProperty("mkdPerEur").GetDecimal());
            Assert.Equal(await PersonalEntrySteps.ValueAsync(_app, scenario.CoffeeEntryId, "rate_date"), entry.GetProperty("rateDate").GetString());
        }

        [Fact]
        public async Task Export_Entry_ShowsCreatedAtFromTheAppClockAndNullUpdatedAt()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement entry = EntryOf(body, scenario.CoffeeEntryId);

            Assert.Equal(ExpensesApp.ClockStart, entry.GetProperty("createdAt").GetDateTimeOffset());
            Assert.Null(ExportBody.StringOrNull(entry, "updatedAt"));
        }

        [Fact]
        public async Task Export_RecurringIncomes_AreTheActiveRuleAndTheStoppedRule()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            List<JsonElement> rules = ExportBody.ListOf(body.PersonalBudget, "recurringIncomes");

            Assert.Equal(
                new[] { scenario.SalaryId, scenario.StoppedIncomeId }.Order(),
                ExportBody.IdsOf(rules).Order());
        }

        [Fact]
        public async Task Export_ActiveMonthlyRule_ShowsAmountCurrencyTitleDayOfMonthAndStartAndNoStop()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement rule = ExportBody.WithId(ExportBody.ListOf(body.PersonalBudget, "recurringIncomes"), scenario.SalaryId);

            Assert.Equal(4_500_000, rule.GetProperty("amountMinor").GetInt64());
            Assert.Equal("MKD", rule.GetProperty("currency").GetString());
            Assert.Equal(ExportScenarioSteps.SalaryTitle, rule.GetProperty("title").GetString());
            Assert.Equal("Monthly", rule.GetProperty("frequency").GetString());
            Assert.Equal(15, rule.GetProperty("dayOfMonth").GetInt32());
            Assert.Equal(JsonValueKind.Null, rule.GetProperty("dayOfWeek").ValueKind);
            Assert.Equal("2026-10-01", rule.GetProperty("startsOn").GetString());
            Assert.Null(ExportBody.StringOrNull(rule, "endsOn"));
            Assert.Null(ExportBody.StringOrNull(rule, "stoppedAt"));
        }

        [Fact]
        public async Task Export_StoppedWeeklyRule_ShowsDayOfWeekAndStoppedAt()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            JsonElement rule = ExportBody.WithId(ExportBody.ListOf(body.PersonalBudget, "recurringIncomes"), scenario.StoppedIncomeId);

            Assert.Equal("Weekly", rule.GetProperty("frequency").GetString());
            Assert.Equal(3, rule.GetProperty("dayOfWeek").GetInt32());
            Assert.Equal(JsonValueKind.Null, rule.GetProperty("dayOfMonth").ValueKind);
            Assert.NotNull(ExportBody.StringOrNull(rule, "stoppedAt"));
        }

        [Fact]
        public async Task Export_BudgetLimits_AreEveryStoredRowOfTheExporterIncludingTheNoLimitRow()
        {
            ExportBody body = await _app.ExporterBodyAsync();

            List<JsonElement> limits = ExportBody.ListOf(body.PersonalBudget, "budgetLimits");

            Assert.Equal(3, limits.Count);
            Assert.Single(limits, limit => IsLimit(limit, null, "2026-10-01", 800_000));
            Assert.Single(limits, limit => IsLimit(limit, PersonalEntryRequests.BuiltInFoodId, "2026-10-01", 300_000));
            Assert.Single(limits, limit => IsLimit(limit, PersonalEntryRequests.BuiltInFoodId, "2026-11-01", null));
        }

        [Fact]
        public async Task Export_DuplicateAnswers_ShowTheExportersAnswerOnly()
        {
            ExportScenario scenario = await _app.ScenarioAsync();
            ExportBody body = await _app.ExporterBodyAsync();

            List<JsonElement> answers = ExportBody.ListOf(body.PersonalBudget, "duplicateAnswers");

            JsonElement answer = Assert.Single(answers);
            Assert.Equal("Same", answer.GetProperty("answer").GetString());
            Assert.Equal(scenario.FoodEntryId, answer.GetProperty("personalEntryId").GetGuid());
            Assert.Equal(scenario.DinnerExpenseId, answer.GetProperty("expenseId").GetGuid());
        }

        private static JsonElement EntryOf(ExportBody body, Guid entryId)
        {
            return ExportBody.WithId(ExportBody.ListOf(body.PersonalBudget, "entries"), entryId);
        }

        private static bool IsLimit(JsonElement limit, Guid? categoryId, string effectiveFrom, long? limitMinor)
        {
            JsonElement limitValue = limit.GetProperty("limitMinor");
            long? actualLimit = limitValue.ValueKind == JsonValueKind.Null ? null : limitValue.GetInt64();

            return ExportBody.GuidOrNull(limit, "categoryId") == categoryId
                && limit.GetProperty("effectiveFrom").GetString() == effectiveFrom
                && actualLimit == limitMinor;
        }
    }
}
