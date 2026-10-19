using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.PersonalCategories;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.PersonalEntries
{
    public class ListPersonalEntriesTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly string[] MonthFieldNames = ["entries", "month", "totals"];
        private static readonly string[] CurrencyTotalFieldNames = ["categories", "currency", "totalMinor"];
        private static readonly string[] CategoryTotalFieldNames = ["category", "categoryId", "count", "totalMinor"];

        [Fact]
        public async Task List_NoEntries_Answers200WithTheMonthAndEmptyListsOnly()
        {
            SignedInUser user = await _app.SignUpAsync();

            JsonElement body = await PersonalEntrySteps.MonthAsync(user.Client, "2026-10");

            Assert.Equal(MonthFieldNames, body.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
            Assert.Equal("2026-10", body.GetProperty("month").GetString());
            Assert.Equal(0, body.GetProperty("entries").GetArrayLength());
            Assert.Equal(0, body.GetProperty("totals").GetArrayLength());
        }

        [Fact]
        public async Task List_EntriesAroundTheMonth_ReturnsOnlyTheFirstToTheLastDayOfThatMonthNewestFirst()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid lastOfSeptember = await AddOnAsync(user, "2026-09-30");
            Guid firstOfOctober = await AddOnAsync(user, "2026-10-01");
            Guid lastOfOctober = await AddOnAsync(user, "2026-10-31");
            Guid firstOfNovember = await AddOnAsync(user, "2026-11-01");

            Assert.Equal([lastOfOctober, firstOfOctober], await PersonalEntrySteps.EntryIdsOfMonthAsync(user.Client, "2026-10"));
            Assert.Equal([lastOfSeptember], await PersonalEntrySteps.EntryIdsOfMonthAsync(user.Client, "2026-09"));
            Assert.Equal([firstOfNovember], await PersonalEntrySteps.EntryIdsOfMonthAsync(user.Client, "2026-11"));
        }

        [Fact]
        public async Task List_EntriesOnTheLastDayOfFebruaryInALeapYear_AreInFebruary()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid leapDay = await AddOnAsync(user, "2024-02-29");
            await AddOnAsync(user, "2024-03-01");

            Assert.Equal([leapDay], await PersonalEntrySteps.EntryIdsOfMonthAsync(user.Client, "2024-02"));
        }

        [Fact]
        public async Task List_EntriesOnTheSameDay_ComeNewestAddedFirst()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid first = await AddOnAsync(user, "2026-10-04");
            _app.Clock.Advance(TimeSpan.FromMinutes(1));
            Guid second = await AddOnAsync(user, "2026-10-04");
            _app.Clock.Advance(TimeSpan.FromMinutes(1));
            Guid third = await AddOnAsync(user, "2026-10-04");

            Assert.Equal([third, second, first], await PersonalEntrySteps.EntryIdsOfMonthAsync(user.Client));
        }

        [Fact]
        public async Task List_AnEntry_HasTheSameShapeAsTheAddAnswer()
        {
            SignedInUser user = await _app.SignUpAsync();
            HttpResponseMessage added = await PersonalEntryRequests.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, note: "Lunch"));
            JsonElement addedBody = await AuthRequests.ReadJsonAsync(added);

            JsonElement body = await PersonalEntrySteps.MonthAsync(user.Client);

            JsonElement listed = Assert.Single(body.GetProperty("entries").EnumerateArray());
            Assert.Equal(addedBody.GetRawText(), listed.GetRawText());
        }

        [Fact]
        public async Task List_Totals_ArePerCurrencyWithMkdAndEurApartAndPerCategoryWithACount()
        {
            SignedInUser user = await _app.SignUpAsync();
            await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, 100_000, "MKD"));
            await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, 50_000, "MKD"));
            await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInTransportId, 30_000, "MKD"));
            await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, 1_550, "EUR"));

            JsonElement totals = (await PersonalEntrySteps.MonthAsync(user.Client)).GetProperty("totals");

            Assert.Equal(2, totals.GetArrayLength());
            JsonElement mkd = totals[0];
            Assert.Equal(CurrencyTotalFieldNames, mkd.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
            Assert.Equal("MKD", mkd.GetProperty("currency").GetString());
            Assert.Equal(180_000, mkd.GetProperty("totalMinor").GetInt64());
            Assert.Equal(
                [(PersonalEntryRequests.BuiltInFoodId, 150_000L, 2), (PersonalEntryRequests.BuiltInTransportId, 30_000L, 1)],
                CategoryTotalsOf(mkd));
            JsonElement eur = totals[1];
            Assert.Equal("EUR", eur.GetProperty("currency").GetString());
            Assert.Equal(1_550, eur.GetProperty("totalMinor").GetInt64());
            Assert.Equal([(PersonalEntryRequests.BuiltInFoodId, 1_550L, 1)], CategoryTotalsOf(eur));
        }

        [Fact]
        public async Task List_CategoryTotal_EmbedsTheCategory()
        {
            SignedInUser user = await _app.SignUpAsync();
            await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));

            JsonElement categoryTotal = (await PersonalEntrySteps.MonthAsync(user.Client)).GetProperty("totals")[0].GetProperty("categories")[0];

            Assert.Equal(CategoryTotalFieldNames, categoryTotal.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
            Assert.Equal("food", categoryTotal.GetProperty("category").GetProperty("key").GetString());
            Assert.Equal("orange", categoryTotal.GetProperty("category").GetProperty("color").GetString());
        }

        [Fact]
        public async Task List_OnlyEurEntries_HasOnlyAnEurTotal()
        {
            SignedInUser user = await _app.SignUpAsync();
            await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, 2_000, "EUR"));

            JsonElement totals = (await PersonalEntrySteps.MonthAsync(user.Client)).GetProperty("totals");

            JsonElement eur = Assert.Single(totals.EnumerateArray());
            Assert.Equal("EUR", eur.GetProperty("currency").GetString());
            Assert.Equal(2_000, eur.GetProperty("totalMinor").GetInt64());
        }

        [Fact]
        public async Task List_TotalsOfOtherMonths_AreNotCounted()
        {
            SignedInUser user = await _app.SignUpAsync();
            await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, 100_000, entryDate: "2026-10-31"));
            await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, 70_000, entryDate: "2026-11-01"));
            await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, 40_000, entryDate: "2026-09-30"));

            JsonElement mkd = Assert.Single((await PersonalEntrySteps.MonthAsync(user.Client)).GetProperty("totals").EnumerateArray());

            Assert.Equal(100_000, mkd.GetProperty("totalMinor").GetInt64());
        }

        [Fact]
        public async Task List_DeletedEntries_AreLeftOutOfTheListAndTheTotals()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid kept = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, 100_000));
            await PersonalEntrySteps.AddAndDeleteAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, 70_000));
            await PersonalEntrySteps.AddAndDeleteAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInTransportId, 5_000, "EUR"));

            JsonElement body = await PersonalEntrySteps.MonthAsync(user.Client);

            Assert.Equal([kept], [.. body.GetProperty("entries").EnumerateArray().Select(entry => entry.GetProperty("id").GetGuid())]);
            JsonElement mkd = Assert.Single(body.GetProperty("totals").EnumerateArray());
            Assert.Equal(100_000, mkd.GetProperty("totalMinor").GetInt64());
            Assert.Equal([(PersonalEntryRequests.BuiltInFoodId, 100_000L, 1)], CategoryTotalsOf(mkd));
        }

        [Fact]
        public async Task List_AnEntryWhoseCategoryWasArchivedSince_StillShowsItWithTheArchivedCategory()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid pets = await PersonalCategorySteps.CreateAsync(user.Client, "Pets");
            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(pets, 80_000));
            await PersonalCategoryRequests.ArchiveAsync(user.Client, pets);

            JsonElement body = await PersonalEntrySteps.MonthAsync(user.Client);

            JsonElement entry = Assert.Single(body.GetProperty("entries").EnumerateArray());
            Assert.Equal(entryId, entry.GetProperty("id").GetGuid());
            Assert.Equal("Pets", entry.GetProperty("category").GetProperty("name").GetString());
            Assert.True(entry.GetProperty("category").GetProperty("archived").GetBoolean());
            JsonElement categoryTotal = Assert.Single(body.GetProperty("totals")[0].GetProperty("categories").EnumerateArray());
            Assert.Equal(pets, categoryTotal.GetProperty("categoryId").GetGuid());
            Assert.True(categoryTotal.GetProperty("category").GetProperty("archived").GetBoolean());
        }

        [Fact]
        public async Task List_EntriesOfAnotherUser_AreNeverShownOrCounted()
        {
            SignedInUser user = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            Guid own = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, 100_000));
            Guid others = await PersonalEntrySteps.AddAsync(other.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, 900_000));

            JsonElement body = await PersonalEntrySteps.MonthAsync(user.Client);

            Assert.Equal([own], [.. body.GetProperty("entries").EnumerateArray().Select(entry => entry.GetProperty("id").GetGuid())]);
            Assert.Equal(100_000, body.GetProperty("totals")[0].GetProperty("totalMinor").GetInt64());
            Assert.Equal([others], await PersonalEntrySteps.EntryIdsOfMonthAsync(other.Client));
        }

        [Theory]
        [InlineData("2026-13")]
        [InlineData("2026-00")]
        [InlineData("2026-1")]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("2026-10-01")]
        [InlineData("26-10")]
        [InlineData("2026/10")]
        [InlineData(" 2026-10")]
        public async Task List_MonthThatIsNotYearDashMonth_Answers400MonthInvalid(string month)
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.ListAsync(user.Client, month);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_MONTH_INVALID);
        }

        [Fact]
        public async Task List_WithoutAMonth_Answers400MonthInvalid()
        {
            SignedInUser user = await _app.SignUpAsync();

            HttpResponseMessage response = await PersonalEntryRequests.ListWithoutMonthAsync(user.Client);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_MONTH_INVALID);
        }

        private static List<(Guid CategoryId, long TotalMinor, int Count)> CategoryTotalsOf(JsonElement currencyTotal)
        {
            return [.. currencyTotal.GetProperty("categories").EnumerateArray().Select(category => (
                category.GetProperty("categoryId").GetGuid(),
                category.GetProperty("totalMinor").GetInt64(),
                category.GetProperty("count").GetInt32()))];
        }

        private static Task<Guid> AddOnAsync(SignedInUser user, string entryDate)
        {
            return PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, entryDate: entryDate));
        }
    }
}
