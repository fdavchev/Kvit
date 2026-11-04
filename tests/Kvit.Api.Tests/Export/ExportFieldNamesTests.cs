using System.Text.Json;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.Export
{
    public class ExportFieldNamesTests(ExportApp _app) : IClassFixture<ExportApp>
    {
        [Fact]
        public async Task Export_Groups_HaveExactlyThePinnedFieldNames()
        {
            ExportBody body = await _app.ExporterBodyAsync();

            AssertEveryItemHasExactly(body.Groups, ExportRequests.GroupFieldNames);
        }

        [Fact]
        public async Task Export_Memberships_HaveExactlyThePinnedFieldNames()
        {
            ExportBody body = await _app.ExporterBodyAsync();

            List<JsonElement> memberships = [.. body.Groups.Select(group => group.GetProperty("membership"))];

            AssertEveryItemHasExactly(memberships, ExportRequests.MembershipFieldNames);
        }

        [Fact]
        public async Task Export_Members_HaveExactlyThePinnedFieldNames()
        {
            ExportBody body = await _app.ExporterBodyAsync();

            AssertEveryItemHasExactly(ItemsOfEveryGroup(body, "members"), ExportRequests.MemberFieldNames);
        }

        [Fact]
        public async Task Export_Expenses_HaveExactlyThePinnedFieldNames()
        {
            ExportBody body = await _app.ExporterBodyAsync();

            AssertEveryItemHasExactly(ItemsOfEveryGroup(body, "expenses"), ExportRequests.ExpenseFieldNames);
        }

        [Fact]
        public async Task Export_Shares_HaveExactlyThePinnedFieldNames()
        {
            ExportBody body = await _app.ExporterBodyAsync();
            List<JsonElement> shares = [.. ItemsOfEveryGroup(body, "expenses").SelectMany(expense => ExportBody.ListOf(expense, "shares"))];

            AssertEveryItemHasExactly(shares, ExportRequests.ShareFieldNames);
        }

        [Fact]
        public async Task Export_Payments_HaveExactlyThePinnedFieldNames()
        {
            ExportBody body = await _app.ExporterBodyAsync();

            AssertEveryItemHasExactly(ItemsOfEveryGroup(body, "payments"), ExportRequests.PaymentFieldNames);
        }

        [Fact]
        public async Task Export_Categories_HaveExactlyThePinnedFieldNames()
        {
            ExportBody body = await _app.ExporterBodyAsync();

            AssertEveryItemHasExactly(ExportBody.ListOf(body.PersonalBudget, "categories"), ExportRequests.CategoryFieldNames);
        }

        [Fact]
        public async Task Export_Entries_HaveExactlyThePinnedFieldNames()
        {
            ExportBody body = await _app.ExporterBodyAsync();

            AssertEveryItemHasExactly(ExportBody.ListOf(body.PersonalBudget, "entries"), ExportRequests.EntryFieldNames);
        }

        [Fact]
        public async Task Export_RecurringIncomes_HaveExactlyThePinnedFieldNames()
        {
            ExportBody body = await _app.ExporterBodyAsync();

            AssertEveryItemHasExactly(ExportBody.ListOf(body.PersonalBudget, "recurringIncomes"), ExportRequests.RecurringIncomeFieldNames);
        }

        [Fact]
        public async Task Export_BudgetLimits_HaveExactlyThePinnedFieldNames()
        {
            ExportBody body = await _app.ExporterBodyAsync();

            AssertEveryItemHasExactly(ExportBody.ListOf(body.PersonalBudget, "budgetLimits"), ExportRequests.BudgetLimitFieldNames);
        }

        [Fact]
        public async Task Export_DuplicateAnswers_HaveExactlyThePinnedFieldNames()
        {
            ExportBody body = await _app.ExporterBodyAsync();

            AssertEveryItemHasExactly(ExportBody.ListOf(body.PersonalBudget, "duplicateAnswers"), ExportRequests.DuplicateAnswerFieldNames);
        }

        private static List<JsonElement> ItemsOfEveryGroup(ExportBody body, string listName)
        {
            return [.. body.Groups.SelectMany(group => ExportBody.ListOf(group, listName))];
        }

        private static void AssertEveryItemHasExactly(List<JsonElement> items, string[] expectedFieldNames)
        {
            Assert.NotEmpty(items);
            foreach (JsonElement item in items)
            {
                Assert.Equal(expectedFieldNames, GroupRequests.PropertyNamesOf(item));
            }
        }
    }
}
