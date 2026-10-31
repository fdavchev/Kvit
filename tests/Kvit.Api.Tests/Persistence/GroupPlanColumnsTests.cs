using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class GroupPlanColumnsTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        private const string CheckViolation = "23514";

        [Fact]
        public async Task Columns_AreTheTwoNullablePlanOnesWithTheirTypes()
        {
            List<string?> columns = await _database.QueryAsync(
                "SELECT column_name || ':' || data_type || ':' || is_nullable FROM information_schema.columns "
                + "WHERE table_schema = 'public' AND table_name = 'groups' AND column_name LIKE 'plan\\_%' ORDER BY column_name");

            Assert.Equal(["plan_amount_minor:bigint:YES", "plan_currency:text:YES"], columns);
        }

        [Fact]
        public async Task Columns_HaveNoDatabaseDefault()
        {
            Assert.Equal("bigint", await SchemaQueries.DataTypeAsync(_database, "groups", "plan_amount_minor"));
            Assert.Equal("text", await SchemaQueries.DataTypeAsync(_database, "groups", "plan_currency"));
            Assert.Null(await SchemaQueries.DefaultAsync(_database, "groups", "plan_amount_minor"));
            Assert.Null(await SchemaQueries.DefaultAsync(_database, "groups", "plan_currency"));
        }

        [Fact]
        public async Task Insert_GroupWithoutAPlan_HasBothColumnsNull()
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);

            Assert.Equal(GroupPlanRows.NoPlan, await GroupPlanRows.PlanOfAsync(_database, groupId));
        }

        [Theory]
        [InlineData(100, "MKD")]
        [InlineData(100_000, "MKD")]
        [InlineData(999_999_999_900, "MKD")]
        [InlineData(1, "EUR")]
        [InlineData(150, "EUR")]
        [InlineData(999_999_999_999, "EUR")]
        public async Task Plan_OfAValidAmountAndCurrency_IsAccepted(long amountMinor, string currency)
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);

            await GroupPlanRows.SetAsync(_database, groupId, amountMinor, currency);

            Assert.Equal(GroupPlanRows.TextOf(amountMinor, currency), await GroupPlanRows.PlanOfAsync(_database, groupId));
        }

        [Fact]
        public async Task Plan_SetBackToNullInBothColumns_IsAccepted()
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);
            await GroupPlanRows.SetAsync(_database, groupId, 100_000, "MKD");

            await GroupPlanRows.SetAsync(_database, groupId, null, null);

            Assert.Equal(GroupPlanRows.NoPlan, await GroupPlanRows.PlanOfAsync(_database, groupId));
        }

        [Theory]
        [InlineData(100_000L, null)]
        [InlineData(null, "MKD")]
        [InlineData(null, "EUR")]
        public async Task Plan_WithOnlyOneOfTheTwoColumnsSet_ViolatesTheCheckConstraint(long? amountMinor, string? currency)
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);

            await RejectedInsert.AssertAsync(() => GroupPlanRows.SetAsync(_database, groupId, amountMinor, currency), CheckViolation);

            Assert.Equal(GroupPlanRows.NoPlan, await GroupPlanRows.PlanOfAsync(_database, groupId));
        }

        [Theory]
        [InlineData(0, "MKD")]
        [InlineData(-100, "MKD")]
        [InlineData(0, "EUR")]
        [InlineData(-1, "EUR")]
        [InlineData(long.MinValue, "EUR")]
        public async Task Plan_ThatIsNotAboveZero_ViolatesTheCheckConstraint(long amountMinor, string currency)
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);

            await RejectedInsert.AssertAsync(() => GroupPlanRows.SetAsync(_database, groupId, amountMinor, currency), CheckViolation);
        }

        [Theory]
        [InlineData(1_000_000_000_000, "EUR")]
        [InlineData(1_000_000_000_000, "MKD")]
        [InlineData(long.MaxValue, "EUR")]
        [InlineData(long.MaxValue - 7, "MKD")]
        public async Task Plan_AboveTheEntryBound_ViolatesTheCheckConstraint(long amountMinor, string currency)
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);

            await RejectedInsert.AssertAsync(() => GroupPlanRows.SetAsync(_database, groupId, amountMinor, currency), CheckViolation);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(99)]
        [InlineData(101)]
        [InlineData(150)]
        [InlineData(150_050)]
        [InlineData(999_999_999_999)]
        public async Task Plan_InDenarsThatIsNotAWholeNumberOfDenars_ViolatesTheCheckConstraint(long amountMinor)
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);

            await RejectedInsert.AssertAsync(() => GroupPlanRows.SetAsync(_database, groupId, amountMinor, "MKD"), CheckViolation);
        }

        [Theory]
        [InlineData("USD")]
        [InlineData("mkd")]
        [InlineData("eur")]
        [InlineData("Eur")]
        [InlineData("")]
        [InlineData("MKD ")]
        public async Task Plan_InACurrencyOutsideTheTwoKnownNames_ViolatesTheCheckConstraint(string currency)
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);

            await RejectedInsert.AssertAsync(() => GroupPlanRows.SetAsync(_database, groupId, 100_000, currency), CheckViolation);
        }

        [Fact]
        public async Task Plan_Rejected_LeavesTheEarlierPlanInPlace()
        {
            Guid groupId = await GroupRows.InsertGroupWithoutMembersAsync(_database);
            await GroupPlanRows.SetAsync(_database, groupId, 100_000, "MKD");

            await RejectedInsert.AssertAsync(() => GroupPlanRows.SetAsync(_database, groupId, 150, "MKD"), CheckViolation);

            Assert.Equal("100000:MKD", await GroupPlanRows.PlanOfAsync(_database, groupId));
        }

        [Fact]
        public async Task Migration_GroupPlan_IsInTheMigrationHistoryExactlyOnceAfterDuplicateAnswers()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            List<string> applied = [.. await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)];

            int duplicateAnswers = applied.FindIndex(migration => migration.EndsWith("_DuplicateAnswers", StringComparison.Ordinal));
            int groupPlan = applied.FindIndex(migration => migration.EndsWith("_GroupPlan", StringComparison.Ordinal));
            Assert.Single(applied, migration => migration.EndsWith("_GroupPlan", StringComparison.Ordinal));
            Assert.True(groupPlan > duplicateAnswers, $"The GroupPlan migration is at {groupPlan} but DuplicateAnswers is at {duplicateAnswers}.");
        }

        [Fact]
        public async Task Migration_GroupPlan_LeavesNoMigrationPending()
        {
            using IServiceScope scope = _database.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            IEnumerable<string> pending = await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken);

            Assert.Empty(pending);
        }
    }
}
