using System.Globalization;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.PersonalEntries
{
    public class ListIncomeTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly string[] IncomeTotalFieldNames = ["count", "currency", PersonalEntryRequests.IncomeTotalAmountProperty];

        [Fact]
        public async Task List_MonthWithBothKindsAndBothCurrencies_ReturnsEveryEntryWithItsKind()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid spendingMkd = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, 100_000, "MKD"));
            Guid spendingEur = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, 1_550, "EUR"));
            Guid incomeMkd = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(4_500_000, "MKD"));
            Guid incomeEur = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(20_000, "EUR"));

            JsonElement body = await PersonalEntrySteps.MonthAsync(user.Client);

            Dictionary<Guid, JsonElement> entriesById = body.GetProperty("entries").EnumerateArray().ToDictionary(entry => entry.GetProperty("id").GetGuid());
            Assert.Equal(4, entriesById.Count);
            Assert.Equal("Spending", entriesById[spendingMkd].GetProperty("kind").GetString());
            Assert.Equal("Spending", entriesById[spendingEur].GetProperty("kind").GetString());
            Assert.Equal("Income", entriesById[incomeMkd].GetProperty("kind").GetString());
            Assert.Equal("Income", entriesById[incomeEur].GetProperty("kind").GetString());
        }

        [Fact]
        public async Task List_AnIncomeEntry_HasANullCategoryAndANullCategoryId()
        {
            SignedInUser user = await _app.SignUpAsync();
            await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of());

            JsonElement entry = Assert.Single((await PersonalEntrySteps.MonthAsync(user.Client)).GetProperty("entries").EnumerateArray());

            Assert.Equal(JsonValueKind.Null, entry.GetProperty("categoryId").ValueKind);
            Assert.Equal(JsonValueKind.Null, entry.GetProperty("category").ValueKind);
        }

        [Fact]
        public async Task List_AnIncomeEntry_HasTheSameShapeAsTheAddAnswer()
        {
            SignedInUser user = await _app.SignUpAsync();
            HttpResponseMessage added = await PersonalEntryRequests.AddIncomeAsync(user.Client, IncomeEntryInput.Of(note: "Salary"));
            JsonElement addedBody = await AuthRequests.ReadJsonAsync(added);

            JsonElement body = await PersonalEntrySteps.MonthAsync(user.Client);

            JsonElement listed = Assert.Single(body.GetProperty("entries").EnumerateArray());
            Assert.Equal(addedBody.GetRawText(), listed.GetRawText());
        }

        [Fact]
        public async Task List_MonthWithBothKindsAndBothCurrencies_KeepsTheSpendingTotalsExactlyAsTheyWere()
        {
            SignedInUser user = await _app.SignUpAsync();
            await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, 100_000, "MKD"));
            await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, 50_000, "MKD"));
            await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInTransportId, 30_000, "MKD"));
            await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, 1_550, "EUR"));
            await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(4_500_000, "MKD"));
            await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(20_000, "EUR"));

            JsonElement totals = (await PersonalEntrySteps.MonthAsync(user.Client)).GetProperty("totals");

            Assert.Equal(2, totals.GetArrayLength());
            Assert.Equal("MKD", totals[0].GetProperty("currency").GetString());
            Assert.Equal(180_000, totals[0].GetProperty("totalMinor").GetInt64());
            Assert.Equal(2, totals[0].GetProperty("categories").GetArrayLength());
            Assert.Equal("EUR", totals[1].GetProperty("currency").GetString());
            Assert.Equal(1_550, totals[1].GetProperty("totalMinor").GetInt64());
            Assert.Equal(1, totals[1].GetProperty("categories").GetArrayLength());
        }

        [Fact]
        public async Task List_MonthWithBothKindsAndBothCurrencies_ReturnsIncomeTotalsPerCurrencyMkdThenEurWithACount()
        {
            SignedInUser user = await _app.SignUpAsync();
            await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId, 100_000, "MKD"));
            await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(4_500_000, "MKD"));
            await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(1_000_000, "MKD"));
            await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(20_000, "EUR"));

            JsonElement incomeTotals = (await PersonalEntrySteps.MonthAsync(user.Client)).GetProperty("incomeTotals");

            Assert.Equal(2, incomeTotals.GetArrayLength());
            JsonElement mkd = incomeTotals[0];
            Assert.Equal(IncomeTotalFieldNames, mkd.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
            Assert.Equal("MKD", mkd.GetProperty("currency").GetString());
            Assert.Equal(5_500_000, mkd.GetProperty(PersonalEntryRequests.IncomeTotalAmountProperty).GetInt64());
            Assert.Equal(2, mkd.GetProperty("count").GetInt32());
            JsonElement eur = incomeTotals[1];
            Assert.Equal("EUR", eur.GetProperty("currency").GetString());
            Assert.Equal(20_000, eur.GetProperty(PersonalEntryRequests.IncomeTotalAmountProperty).GetInt64());
            Assert.Equal(1, eur.GetProperty("count").GetInt32());
        }

        [Fact]
        public async Task List_EurIncomeEnteredBeforeMkdIncome_StillGivesMkdFirstInTheIncomeTotals()
        {
            SignedInUser user = await _app.SignUpAsync();
            await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(20_000, "EUR"));
            await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(4_500_000, "MKD"));

            JsonElement incomeTotals = (await PersonalEntrySteps.MonthAsync(user.Client)).GetProperty("incomeTotals");

            Assert.Equal(["MKD", "EUR"], [.. incomeTotals.EnumerateArray().Select(total => total.GetProperty("currency").GetString())]);
        }

        [Fact]
        public async Task List_OnlyEurIncome_HasOnlyAnEurIncomeTotal()
        {
            SignedInUser user = await _app.SignUpAsync();
            await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(20_000, "EUR"));

            JsonElement incomeTotals = (await PersonalEntrySteps.MonthAsync(user.Client)).GetProperty("incomeTotals");

            JsonElement eur = Assert.Single(incomeTotals.EnumerateArray());
            Assert.Equal("EUR", eur.GetProperty("currency").GetString());
            Assert.Equal(20_000, eur.GetProperty(PersonalEntryRequests.IncomeTotalAmountProperty).GetInt64());
        }

        [Fact]
        public async Task List_OnlyIncome_HasNoSpendingTotalsAndTheIncomeTotal()
        {
            SignedInUser user = await _app.SignUpAsync();
            await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(4_500_000, "MKD"));

            JsonElement body = await PersonalEntrySteps.MonthAsync(user.Client);

            Assert.Equal(0, body.GetProperty("totals").GetArrayLength());
            JsonElement mkd = Assert.Single(body.GetProperty("incomeTotals").EnumerateArray());
            Assert.Equal(4_500_000, mkd.GetProperty(PersonalEntryRequests.IncomeTotalAmountProperty).GetInt64());
        }

        [Fact]
        public async Task List_OnlySpending_HasAnEmptyIncomeTotals()
        {
            SignedInUser user = await _app.SignUpAsync();
            await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));

            JsonElement body = await PersonalEntrySteps.MonthAsync(user.Client);

            Assert.Equal(0, body.GetProperty("incomeTotals").GetArrayLength());
        }

        [Fact]
        public async Task List_IncomeTotalsOfOtherMonths_AreNotCounted()
        {
            SignedInUser user = await _app.SignUpAsync();
            await AddIncomeOnAsync(user, "2026-10-31", 1_000_000);
            await AddIncomeOnAsync(user, "2026-11-01", 700_000);
            await AddIncomeOnAsync(user, "2026-09-30", 400_000);

            JsonElement mkd = Assert.Single((await PersonalEntrySteps.MonthAsync(user.Client)).GetProperty("incomeTotals").EnumerateArray());

            Assert.Equal(1_000_000, mkd.GetProperty(PersonalEntryRequests.IncomeTotalAmountProperty).GetInt64());
            Assert.Equal(1, mkd.GetProperty("count").GetInt32());
        }

        [Fact]
        public async Task List_DeletedIncome_IsLeftOutOfTheListAndTheIncomeTotals()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid kept = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(1_000_000));
            await PersonalEntrySteps.AddIncomeAndDeleteAsync(user.Client, IncomeEntryInput.Of(700_000));
            await PersonalEntrySteps.AddIncomeAndDeleteAsync(user.Client, IncomeEntryInput.Of(5_000, "EUR"));

            JsonElement body = await PersonalEntrySteps.MonthAsync(user.Client);

            Assert.Equal([kept], [.. body.GetProperty("entries").EnumerateArray().Select(entry => entry.GetProperty("id").GetGuid())]);
            JsonElement mkd = Assert.Single(body.GetProperty("incomeTotals").EnumerateArray());
            Assert.Equal(1_000_000, mkd.GetProperty(PersonalEntryRequests.IncomeTotalAmountProperty).GetInt64());
            Assert.Equal(1, mkd.GetProperty("count").GetInt32());
        }

        [Fact]
        public async Task List_IncomeOfAnotherUser_IsNeverShownOrCounted()
        {
            SignedInUser user = await _app.SignUpAsync();
            SignedInUser other = await _app.SignUpAsync("Bojan");
            Guid own = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(1_000_000));
            Guid others = await PersonalEntrySteps.AddIncomeAsync(other.Client, IncomeEntryInput.Of(9_000_000));

            JsonElement body = await PersonalEntrySteps.MonthAsync(user.Client);

            Assert.Equal([own], [.. body.GetProperty("entries").EnumerateArray().Select(entry => entry.GetProperty("id").GetGuid())]);
            JsonElement mkd = Assert.Single(body.GetProperty("incomeTotals").EnumerateArray());
            Assert.Equal(1_000_000, mkd.GetProperty(PersonalEntryRequests.IncomeTotalAmountProperty).GetInt64());
            Assert.Equal([others], await PersonalEntrySteps.EntryIdsOfMonthAsync(other.Client));
        }

        [Fact]
        public async Task List_EntriesOfBothKindsOnTheSameDay_ComeNewestAddedFirst()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid first = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));
            _app.Clock.Advance(TimeSpan.FromMinutes(1));
            Guid second = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of());
            _app.Clock.Advance(TimeSpan.FromMinutes(1));
            Guid third = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));

            Assert.Equal([third, second, first], await PersonalEntrySteps.EntryIdsOfMonthAsync(user.Client));
        }

        [Fact]
        public async Task List_EntriesOfBothKindsOnDifferentDays_ComeNewestDateFirst()
        {
            SignedInUser user = await _app.SignUpAsync();
            Guid income = await AddIncomeOnAsync(user, "2026-10-02");
            Guid spending = await AddSpendingOnAsync(user, "2026-10-03");
            Guid laterIncome = await AddIncomeOnAsync(user, "2026-10-04");

            Assert.Equal([laterIncome, spending, income], await PersonalEntrySteps.EntryIdsOfMonthAsync(user.Client));
        }

        private async Task<Guid> AddIncomeOnAsync(SignedInUser user, string entryDate, long amountMinor = IncomeEntryInput.Salary)
        {
            Guid entryId = await PersonalEntrySteps.AddIncomeAsync(user.Client, IncomeEntryInput.Of(amountMinor));
            await PersonalEntrySteps.SetEntryDateAsync(_app, entryId, DateOnly.ParseExact(entryDate, "yyyy-MM-dd", CultureInfo.InvariantCulture));

            return entryId;
        }

        private async Task<Guid> AddSpendingOnAsync(SignedInUser user, string entryDate)
        {
            Guid entryId = await PersonalEntrySteps.AddAsync(user.Client, EntryInput.Of(PersonalEntryRequests.BuiltInFoodId));
            await PersonalEntrySteps.SetEntryDateAsync(_app, entryId, DateOnly.ParseExact(entryDate, "yyyy-MM-dd", CultureInfo.InvariantCulture));

            return entryId;
        }
    }
}
