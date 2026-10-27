using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.BudgetCurrency;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.RecurringIncomes;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.BudgetLimits
{
    public class LimitHistoryTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string InSeptember = "2026-09-10T12:00:00Z";
        private const string InOctober = "2026-10-12T12:00:00Z";
        private const string InNovember = "2026-11-08T12:00:00Z";
        private const string InDecember = "2026-12-03T12:00:00Z";
        private const string InLateNovember = "2026-11-25T12:00:00Z";
        private const long TenThousandDenars = 1_000_000;
        private const long NineThousandDenars = 900_000;
        private const long EightThousandDenars = 800_000;
        private const long SixThousandDenars = 600_000;
        private const long FiveThousandDenars = 500_000;

        [Fact]
        public async Task Month_LimitLoweredInOctober_SeptemberKeepsTheOldLimitAndOctoberShowsTheNewOneWithTheSameSpending()
        {
            SignedInUser user = await UserWithFoodSpendingInSeptemberAndOctoberAsync();
            await SetBothLimitsAsync(user, InSeptember, TenThousandDenars);

            await SetBothLimitsAsync(user, InOctober, EightThousandDenars);

            JsonElement september = await BudgetLimitSteps.LimitsAsync(user.Client, "2026-09");
            JsonElement october = await BudgetLimitSteps.LimitsAsync(user.Client, "2026-10");
            BudgetLimitSteps.AssertRow(september.GetProperty("month"), null, TenThousandDenars, NineThousandDenars, 0, 0);
            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(september, BudgetLimitSteps.Food), BudgetLimitSteps.Food, TenThousandDenars, NineThousandDenars, 0, 0);
            BudgetLimitSteps.AssertRow(october.GetProperty("month"), null, EightThousandDenars, NineThousandDenars, 100_000, 0);
            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(october, BudgetLimitSteps.Food), BudgetLimitSteps.Food, EightThousandDenars, NineThousandDenars, 100_000, 0);
            Assert.Equal(["2026-09-01:1000000", "2026-10-01:800000"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, null));
            Assert.Equal(["2026-09-01:1000000", "2026-10-01:800000"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, BudgetLimitSteps.Food));
        }

        [Fact]
        public async Task Month_LimitWithNoChangeAfterwards_KeepsApplyingToLaterMonths()
        {
            SignedInUser user = await UserWithFoodSpendingInSeptemberAndOctoberAsync();
            await SetBothLimitsAsync(user, InSeptember, TenThousandDenars);
            await SetBothLimitsAsync(user, InOctober, EightThousandDenars);

            JsonElement november = await BudgetLimitSteps.LimitsAsync(user.Client, "2026-11");
            JsonElement nextYear = await BudgetLimitSteps.LimitsAsync(user.Client, "2027-06");

            BudgetLimitSteps.AssertRow(november.GetProperty("month"), null, EightThousandDenars, 0, 0, 0);
            BudgetLimitSteps.AssertRow(nextYear.GetProperty("month"), null, EightThousandDenars, 0, 0, 0);
        }

        [Fact]
        public async Task Month_LimitRemovedInNovember_OctoberStillShowsItAndNovemberAndLaterShowNone()
        {
            SignedInUser user = await UserWithFoodSpendingInSeptemberAndOctoberAsync();
            await SetBothLimitsAsync(user, InSeptember, TenThousandDenars);
            await SetBothLimitsAsync(user, InOctober, EightThousandDenars);

            await RemoveBothLimitsAsync(user, InNovember);

            JsonElement september = await BudgetLimitSteps.LimitsAsync(user.Client, "2026-09");
            JsonElement october = await BudgetLimitSteps.LimitsAsync(user.Client, "2026-10");
            BudgetLimitSteps.AssertRow(september.GetProperty("month"), null, TenThousandDenars, NineThousandDenars, 0, 0);
            BudgetLimitSteps.AssertRow(october.GetProperty("month"), null, EightThousandDenars, NineThousandDenars, 100_000, 0);
            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(october, BudgetLimitSteps.Food), BudgetLimitSteps.Food, EightThousandDenars, NineThousandDenars, 100_000, 0);
            BudgetLimitSteps.AssertNoLimits(await BudgetLimitSteps.LimitsAsync(user.Client, "2026-11"));
            BudgetLimitSteps.AssertNoLimits(await BudgetLimitSteps.LimitsAsync(user.Client, "2026-12"));
            BudgetLimitSteps.AssertNoLimits(await BudgetLimitSteps.LimitsAsync(user.Client, "2027-03"));
            Assert.Equal(["2026-09-01:1000000", "2026-10-01:800000", "2026-11-01:none"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, null));
            Assert.Equal(["2026-09-01:1000000", "2026-10-01:800000", "2026-11-01:none"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, BudgetLimitSteps.Food));
        }

        [Fact]
        public async Task Month_MonthsBeforeTheFirstLimit_ShowNone()
        {
            SignedInUser user = await UserWithFoodSpendingInSeptemberAndOctoberAsync();

            await SetBothLimitsAsync(user, InOctober, EightThousandDenars);

            BudgetLimitSteps.AssertNoLimits(await BudgetLimitSteps.LimitsAsync(user.Client, "2026-09"));
            BudgetLimitSteps.AssertNoLimits(await BudgetLimitSteps.LimitsAsync(user.Client, "2026-08"));
            BudgetLimitSteps.AssertNoLimits(await BudgetLimitSteps.LimitsAsync(user.Client, "2025-01"));
            JsonElement october = await BudgetLimitSteps.LimitsAsync(user.Client, "2026-10");
            BudgetLimitSteps.AssertRow(october.GetProperty("month"), null, EightThousandDenars, NineThousandDenars, 100_000, 0);
        }

        [Fact]
        public async Task Set_TwiceInTheSameMonth_KeepsOneRowForThatMonthWithTheLastValue()
        {
            SignedInUser user = await UserWithFoodSpendingInSeptemberAndOctoberAsync();
            await SetBothLimitsAsync(user, InSeptember, TenThousandDenars);

            await SetBothLimitsAsync(user, InOctober, EightThousandDenars);
            await SetBothLimitsAsync(user, "2026-10-28T12:00:00Z", SixThousandDenars);

            Assert.Equal(["2026-09-01:1000000", "2026-10-01:600000"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, null));
            Assert.Equal(["2026-09-01:1000000", "2026-10-01:600000"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, BudgetLimitSteps.Food));
            JsonElement october = await BudgetLimitSteps.LimitsAsync(user.Client, "2026-10");
            BudgetLimitSteps.AssertRow(october.GetProperty("month"), null, SixThousandDenars, NineThousandDenars, 300_000, 0);
        }

        [Fact]
        public async Task Set_InAMonthAfterAnEarlierOne_NeverEditsTheEarlierRow()
        {
            SignedInUser user = await UserWithFoodSpendingInSeptemberAndOctoberAsync();
            await SetBothLimitsAsync(user, InSeptember, TenThousandDenars);
            string septemberCreated = (await BudgetLimitSteps.TimestampAsync(_app, user.UserId, null, "created_at"))!;
            Assert.Null(await BudgetLimitSteps.TimestampAsync(_app, user.UserId, null, "updated_at"));

            await SetBothLimitsAsync(user, InOctober, EightThousandDenars);

            Assert.Equal(TenThousandDenars, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, null, "2026-09-01"));
            Assert.Equal(TenThousandDenars, await BudgetLimitSteps.StoredLimitAsync(_app, user.UserId, BudgetLimitSteps.Food, "2026-09-01"));
            List<string?> septemberTimestamps = await _app.QueryAsync(
                "SELECT to_char(created_at AT TIME ZONE 'UTC', 'YYYY-MM-DD\"T\"HH24:MI:SS') || '|' || coalesce(updated_at::text, 'none') FROM budget_limits "
                + "WHERE user_id = @user_id AND category_id IS NULL AND effective_from = DATE '2026-09-01'",
                new NpgsqlParameter("user_id", user.UserId));
            Assert.Equal($"{septemberCreated}|none", Assert.Single(septemberTimestamps));
        }

        [Fact]
        public async Task Delete_WhenNothingEarlierIsInForce_LeavesNoRowAtAll()
        {
            SignedInUser user = await UserWithFoodSpendingInSeptemberAndOctoberAsync();
            CatchUpSteps.SetNow(_app, InOctober);
            await BudgetLimitSteps.SetMonthAsync(user, EightThousandDenars);
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, EightThousandDenars);

            await RemoveBothLimitsAsync(user, InOctober);

            Assert.Empty(await BudgetLimitSteps.RowsAsync(_app, user.UserId, null));
            Assert.Empty(await BudgetLimitSteps.RowsAsync(_app, user.UserId, BudgetLimitSteps.Food));
            Assert.Equal(0, await BudgetLimitSteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Delete_WhenNothingExists_Answers204AndLeavesNoRow()
        {
            SignedInUser user = await UserWithFoodSpendingInSeptemberAndOctoberAsync();

            await RemoveBothLimitsAsync(user, InOctober);
            await RemoveBothLimitsAsync(user, InNovember);

            Assert.Equal(0, await BudgetLimitSteps.CountOfUserAsync(_app, user.UserId));
        }

        [Fact]
        public async Task Delete_InTheMonthOfASetWhileAnEarlierLimitIsInForce_ReplacesThatMonthsRowWithNoLimit()
        {
            SignedInUser user = await UserWithFoodSpendingInSeptemberAndOctoberAsync();
            await SetBothLimitsAsync(user, InSeptember, TenThousandDenars);
            await SetBothLimitsAsync(user, InOctober, EightThousandDenars);

            await RemoveBothLimitsAsync(user, "2026-10-20T12:00:00Z");

            Assert.Equal(["2026-09-01:1000000", "2026-10-01:none"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, null));
            BudgetLimitSteps.AssertNoLimits(await BudgetLimitSteps.LimitsAsync(user.Client, "2026-10"));
            JsonElement september = await BudgetLimitSteps.LimitsAsync(user.Client, "2026-09");
            BudgetLimitSteps.AssertRow(september.GetProperty("month"), null, TenThousandDenars, NineThousandDenars, 0, 0);
        }

        [Fact]
        public async Task Delete_TwiceInTheSameMonthWhileAnEarlierLimitIsInForce_KeepsOneNoLimitRow()
        {
            SignedInUser user = await UserWithFoodSpendingInSeptemberAndOctoberAsync();
            await SetBothLimitsAsync(user, InSeptember, TenThousandDenars);

            await RemoveBothLimitsAsync(user, InOctober);
            await RemoveBothLimitsAsync(user, "2026-10-20T12:00:00Z");

            Assert.Equal(["2026-09-01:1000000", "2026-10-01:none"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, null));
            Assert.Equal(["2026-09-01:1000000", "2026-10-01:none"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, BudgetLimitSteps.Food));
        }

        [Fact]
        public async Task Set_AfterARemovalInTheSameMonth_ReplacesTheNoLimitRowWithTheNewLimit()
        {
            SignedInUser user = await UserWithFoodSpendingInSeptemberAndOctoberAsync();
            await SetBothLimitsAsync(user, InSeptember, TenThousandDenars);
            await RemoveBothLimitsAsync(user, InOctober);

            await SetBothLimitsAsync(user, "2026-10-20T12:00:00Z", NineThousandDenars);

            Assert.Equal(["2026-09-01:1000000", "2026-10-01:900000"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, null));
            JsonElement october = await BudgetLimitSteps.LimitsAsync(user.Client, "2026-10");
            BudgetLimitSteps.AssertRow(october.GetProperty("month"), null, NineThousandDenars, NineThousandDenars, 0, 0);
        }

        [Fact]
        public async Task Delete_WhenTheEarlierRowIsAlreadyNoLimit_RemovesTheCurrentMonthsRowInsteadOfWritingAnotherNoLimitRow()
        {
            SignedInUser user = await UserWithFoodSpendingInSeptemberAndOctoberAsync();
            await SetBothLimitsAsync(user, InSeptember, TenThousandDenars);
            await RemoveBothLimitsAsync(user, InOctober);
            await SetBothLimitsAsync(user, InNovember, SixThousandDenars);

            await RemoveBothLimitsAsync(user, InLateNovember);

            Assert.Equal(["2026-09-01:1000000", "2026-10-01:none"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, null));
            Assert.Equal(["2026-09-01:1000000", "2026-10-01:none"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, BudgetLimitSteps.Food));
            BudgetLimitSteps.AssertNoLimits(await BudgetLimitSteps.LimitsAsync(user.Client, "2026-11"));
        }

        [Fact]
        public async Task Month_CategoryLimitRemovedAndSetAgainLater_ShowsTheRightLimitInEveryMonth()
        {
            SignedInUser user = await UserWithFoodSpendingInSeptemberAndOctoberAsync();
            CatchUpSteps.SetNow(_app, InSeptember);
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, FiveThousandDenars);
            CatchUpSteps.SetNow(_app, InOctober);
            Assert.Equal(HttpStatusCode.NoContent, (await BudgetLimitRequests.DeleteCategoryAsync(user.Client, BudgetLimitSteps.Food)).StatusCode);
            CatchUpSteps.SetNow(_app, InDecember);
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, SixThousandDenars);

            JsonElement september = await BudgetLimitSteps.LimitsAsync(user.Client, "2026-09");
            JsonElement october = await BudgetLimitSteps.LimitsAsync(user.Client, "2026-10");
            JsonElement november = await BudgetLimitSteps.LimitsAsync(user.Client, "2026-11");
            JsonElement december = await BudgetLimitSteps.LimitsAsync(user.Client, "2026-12");
            JsonElement january = await BudgetLimitSteps.LimitsAsync(user.Client, "2027-01");

            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(september, BudgetLimitSteps.Food), BudgetLimitSteps.Food, FiveThousandDenars, NineThousandDenars, 400_000, 0);
            BudgetLimitSteps.AssertNoLimits(october);
            BudgetLimitSteps.AssertNoLimits(november);
            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(december, BudgetLimitSteps.Food), BudgetLimitSteps.Food, SixThousandDenars, 0, 0, 0);
            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(january, BudgetLimitSteps.Food), BudgetLimitSteps.Food, SixThousandDenars, 0, 0, 0);
            Assert.Equal(["2026-09-01:500000", "2026-10-01:none", "2026-12-01:600000"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, BudgetLimitSteps.Food));
        }

        [Fact]
        public async Task Month_WholeMonthLimitHasItsOwnHistoryApartFromTheCategories()
        {
            SignedInUser user = await UserWithFoodSpendingInSeptemberAndOctoberAsync();
            await SetBothLimitsAsync(user, InSeptember, TenThousandDenars);
            CatchUpSteps.SetNow(_app, InOctober);
            await BudgetLimitSteps.SetMonthAsync(user, EightThousandDenars);
            CatchUpSteps.SetNow(_app, InNovember);
            Assert.Equal(HttpStatusCode.NoContent, (await BudgetLimitRequests.DeleteMonthAsync(user.Client)).StatusCode);

            JsonElement september = await BudgetLimitSteps.LimitsAsync(user.Client, "2026-09");
            JsonElement october = await BudgetLimitSteps.LimitsAsync(user.Client, "2026-10");
            JsonElement november = await BudgetLimitSteps.LimitsAsync(user.Client, "2026-11");

            BudgetLimitSteps.AssertRow(september.GetProperty("month"), null, TenThousandDenars, NineThousandDenars, 0, 0);
            BudgetLimitSteps.AssertRow(october.GetProperty("month"), null, EightThousandDenars, NineThousandDenars, 100_000, 0);
            Assert.Equal(JsonValueKind.Null, november.GetProperty("month").ValueKind);
            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(september, BudgetLimitSteps.Food), BudgetLimitSteps.Food, TenThousandDenars, NineThousandDenars, 0, 0);
            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(october, BudgetLimitSteps.Food), BudgetLimitSteps.Food, TenThousandDenars, NineThousandDenars, 0, 0);
            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(november, BudgetLimitSteps.Food), BudgetLimitSteps.Food, TenThousandDenars, 0, 0, 0);

            Assert.Equal(["2026-09-01:1000000", "2026-10-01:800000", "2026-11-01:none"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, null));
            Assert.Equal(["2026-09-01:1000000"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, BudgetLimitSteps.Food));
        }

        [Fact]
        public async Task Month_ChangesByOnePerson_NeverReachTheHistoryOfAnotherPerson()
        {
            SignedInUser first = await UserWithFoodSpendingInSeptemberAndOctoberAsync();
            SignedInUser second = await CatchUpSteps.SignUpAsync(_app, "Bojan");
            await SetBothLimitsAsync(second, InSeptember, TenThousandDenars);
            await SetBothLimitsAsync(first, InSeptember, FiveThousandDenars);
            await SetBothLimitsAsync(first, InOctober, SixThousandDenars);
            await RemoveBothLimitsAsync(first, InNovember);

            JsonElement seenBySecondInOctober = await BudgetLimitSteps.LimitsAsync(second.Client, "2026-10");
            JsonElement seenBySecondInNovember = await BudgetLimitSteps.LimitsAsync(second.Client, "2026-11");

            BudgetLimitSteps.AssertRow(seenBySecondInOctober.GetProperty("month"), null, TenThousandDenars, 0, 0, 0);
            BudgetLimitSteps.AssertRow(seenBySecondInNovember.GetProperty("month"), null, TenThousandDenars, 0, 0, 0);
            Assert.Equal(["2026-09-01:1000000"], await BudgetLimitSteps.RowsAsync(_app, second.UserId, null));
            Assert.Equal(["2026-09-01:500000", "2026-10-01:600000", "2026-11-01:none"], await BudgetLimitSteps.RowsAsync(_app, first.UserId, null));
        }

        [Theory]
        [InlineData("Europe/Skopje", "2026-10-31T22:59:00Z", "2026-10-01")]
        [InlineData("Europe/Skopje", "2026-10-31T23:00:00Z", "2026-11-01")]
        [InlineData("Pacific/Kiritimati", "2026-10-31T09:59:00Z", "2026-10-01")]
        [InlineData("Pacific/Kiritimati", "2026-10-31T10:00:00Z", "2026-11-01")]
        [InlineData("Pacific/Honolulu", "2026-11-01T09:59:00Z", "2026-10-01")]
        [InlineData("Pacific/Honolulu", "2026-11-01T10:00:00Z", "2026-11-01")]
        [InlineData("UTC", "2026-10-31T23:59:00Z", "2026-10-01")]
        [InlineData("UTC", "2026-11-01T00:00:00Z", "2026-11-01")]
        public async Task Set_AroundLocalMidnightOfTheFirst_WritesTheRowOfTheMonthInTheUsersTimeZone(string timeZone, string utcInstant, string expectedEffectiveFrom)
        {
            CatchUpSteps.SetNow(_app, utcInstant);
            SignedInUser user = await CatchUpSteps.SignUpInAsync(_app, timeZone);

            await BudgetLimitSteps.SetMonthAsync(user, EightThousandDenars);
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, EightThousandDenars);

            Assert.Equal([$"{expectedEffectiveFrom}:800000"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, null));
            Assert.Equal([$"{expectedEffectiveFrom}:800000"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, BudgetLimitSteps.Food));
        }

        [Fact]
        public async Task Set_TwoUsersInDifferentTimeZonesAtTheSameInstant_EachWritesTheirOwnLocalMonth()
        {
            CatchUpSteps.SetNow(_app, "2026-10-31T23:30:00Z");
            SignedInUser skopje = await CatchUpSteps.SignUpInAsync(_app, CatchUpSteps.Skopje);
            SignedInUser honolulu = await CatchUpSteps.SignUpInAsync(_app, "Pacific/Honolulu", "Bojan");

            await BudgetLimitSteps.SetMonthAsync(skopje, EightThousandDenars);
            await BudgetLimitSteps.SetMonthAsync(honolulu, EightThousandDenars);

            Assert.Equal(["2026-11-01:800000"], await BudgetLimitSteps.RowsAsync(_app, skopje.UserId, null));
            Assert.Equal(["2026-10-01:800000"], await BudgetLimitSteps.RowsAsync(_app, honolulu.UserId, null));
        }

        [Fact]
        public async Task Delete_AroundLocalMidnightOfTheFirst_UsesTheMonthInTheUsersTimeZone()
        {
            CatchUpSteps.SetNow(_app, InSeptember);
            SignedInUser user = await CatchUpSteps.SignUpInAsync(_app, CatchUpSteps.Skopje);
            await BudgetLimitSteps.SetMonthAsync(user, EightThousandDenars);
            CatchUpSteps.SetNow(_app, "2026-09-30T22:30:00Z");

            Assert.Equal(HttpStatusCode.NoContent, (await BudgetLimitRequests.DeleteMonthAsync(user.Client)).StatusCode);

            Assert.Equal(["2026-09-01:800000", "2026-10-01:none"], await BudgetLimitSteps.RowsAsync(_app, user.UserId, null));
        }

        private async Task<SignedInUser> UserWithFoodSpendingInSeptemberAndOctoberAsync()
        {
            CatchUpSteps.SetNow(_app, "2026-11-20T12:00:00Z");
            SignedInUser user = await CatchUpSteps.SignUpAsync(_app);
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, NineThousandDenars, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-09-15");
            await BudgetLimitSteps.AddSpendingAsync(_app, user, BudgetLimitSteps.Food, NineThousandDenars, BudgetCurrencyRequests.Mkd, 61.0000m, "2026-10-15");

            return user;
        }

        private async Task SetBothLimitsAsync(SignedInUser user, string utcInstant, long limitMinor)
        {
            CatchUpSteps.SetNow(_app, utcInstant);
            await BudgetLimitSteps.SetMonthAsync(user, limitMinor);
            await BudgetLimitSteps.SetCategoryAsync(user, BudgetLimitSteps.Food, limitMinor);
        }

        private async Task RemoveBothLimitsAsync(SignedInUser user, string utcInstant)
        {
            CatchUpSteps.SetNow(_app, utcInstant);
            Assert.Equal(HttpStatusCode.NoContent, (await BudgetLimitRequests.DeleteMonthAsync(user.Client)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await BudgetLimitRequests.DeleteCategoryAsync(user.Client, BudgetLimitSteps.Food)).StatusCode);
        }
    }
}
