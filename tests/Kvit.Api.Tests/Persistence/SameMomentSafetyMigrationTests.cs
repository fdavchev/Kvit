using Kvit.Api.Tests.BudgetShares;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.PersonalEntries;
using Kvit.Api.Tests.PossibleDuplicates;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class SameMomentSafetyMigrationTests(ExpensesApp _app) : IClassFixture<ExpensesApp>, IAsyncLifetime
    {
        private const string LastMigrationBeforeTheIndexes = "20261108235156_OwnerTools";
        private const string UniqueViolation = "23505";
        private const string SecondDate = "2026-10-04";
        private const string NewAnswer = "New";

        [Fact]
        public async Task Migrate_OverAnswersThatBreakTheOneMergeRule_KeepsOneMergePerEntryAndPerExpenseAndThenRefusesASecondOne()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid member = group.Member.UserId;
            Guid deletedExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid liveExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, SecondDate);
            Guid sharedExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, SecondDate);
            Guid entryWithAnEndedMerge = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid earlierEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid laterEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid ownerEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Owner);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, deletedExpense);
            await MigrateToAsync(LastMigrationBeforeTheIndexes);
            await DuplicateAnswerRows.InsertAsync(_app, member, entryWithAnEndedMerge, deletedExpense);
            await DuplicateAnswerRows.InsertAsync(_app, member, entryWithAnEndedMerge, liveExpense);
            await DuplicateAnswerRows.InsertAsync(_app, member, earlierEntry, sharedExpense);
            await DuplicateAnswerRows.InsertAsync(_app, member, laterEntry, sharedExpense);
            await DuplicateAnswerRows.InsertAsync(_app, member, laterEntry, liveExpense, NewAnswer);
            await DuplicateAnswerRows.InsertAsync(_app, group.Owner.UserId, ownerEntry, sharedExpense);

            await MigrateToAsync(null);

            Assert.Null(await DuplicateAnswerRows.AnswerOfAsync(_app, member, entryWithAnEndedMerge, deletedExpense));
            Assert.Equal(DuplicateAnswerRequests.SameAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, member, entryWithAnEndedMerge, liveExpense));
            Assert.Equal(DuplicateAnswerRequests.SameAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, member, earlierEntry, sharedExpense));
            Assert.Null(await DuplicateAnswerRows.AnswerOfAsync(_app, member, laterEntry, sharedExpense));
            Assert.Equal(NewAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, member, laterEntry, liveExpense));
            Assert.Equal(DuplicateAnswerRequests.SameAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, group.Owner.UserId, ownerEntry, sharedExpense));
            await RejectedInsert.AssertAsync(() => DuplicateAnswerRows.InsertAsync(_app, member, earlierEntry, deletedExpense), UniqueViolation);
            await RejectedInsert.AssertAsync(() => DuplicateAnswerRows.InsertAsync(_app, member, laterEntry, sharedExpense), UniqueViolation);
            await DuplicateAnswerRows.InsertAsync(_app, member, earlierEntry, liveExpense, NewAnswer);
        }

        [Fact]
        public async Task Migrate_OverAnEmptyDuplicateAnswersTable_AppliesAndCreatesBothIndexes()
        {
            await MigrateToAsync(LastMigrationBeforeTheIndexes);
            await _app.ExecuteAsync("DELETE FROM duplicate_answers");

            await MigrateToAsync(null);

            Assert.Equal("0", Assert.Single(await _app.QueryAsync("SELECT count(*)::text FROM duplicate_answers")));
            Assert.Equal(["ix_duplicate_answers_user_id_expense_id_same", "ix_duplicate_answers_user_id_personal_entry_id_same"], await SameIndexNamesAsync());
        }

        [Fact]
        public async Task Migrate_OverAnswersThatBreakNoRule_KeepsEveryRowUnchanged()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid member = group.Member.UserId;
            Guid firstExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid secondExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, SecondDate);
            Guid firstEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid secondEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await MigrateToAsync(LastMigrationBeforeTheIndexes);
            await DuplicateAnswerRows.InsertAsync(_app, member, firstEntry, firstExpense);
            await DuplicateAnswerRows.InsertAsync(_app, member, secondEntry, secondExpense);
            await DuplicateAnswerRows.InsertAsync(_app, member, firstEntry, secondExpense, NewAnswer);
            await DuplicateAnswerRows.InsertAsync(_app, member, secondEntry, firstExpense, NewAnswer);
            string before = await DuplicateAnswerRows.SnapshotAsync(_app);

            await MigrateToAsync(null);

            Assert.Equal(before, await DuplicateAnswerRows.SnapshotAsync(_app));
        }

        [Fact]
        public async Task Migrate_OverTwoMergesOfTheSameEntryWithTheSameMoment_KeepsTheOneWithTheSmallerId()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid member = group.Member.UserId;
            Guid firstExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid secondExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, SecondDate);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            DateTimeOffset moment = DateTimeOffset.UtcNow;
            await MigrateToAsync(LastMigrationBeforeTheIndexes);
            await DuplicateAnswerRows.InsertWithIdAtAsync(_app, NewLargerId(), member, entryId, firstExpense, moment);
            await DuplicateAnswerRows.InsertWithIdAtAsync(_app, NewSmallerId(), member, entryId, secondExpense, moment);

            await MigrateToAsync(null);

            Assert.Null(await DuplicateAnswerRows.AnswerOfAsync(_app, member, entryId, firstExpense));
            Assert.Equal(DuplicateAnswerRequests.SameAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, member, entryId, secondExpense));
        }

        [Fact]
        public async Task Migrate_OverTwoMergesOfTheSameEntryMadeAtDifferentMoments_KeepsTheEarlierOneEvenWithALargerId()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid member = group.Member.UserId;
            Guid firstExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid secondExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, SecondDate);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            DateTimeOffset earlier = DateTimeOffset.UtcNow.AddMinutes(-10);
            await MigrateToAsync(LastMigrationBeforeTheIndexes);
            await DuplicateAnswerRows.InsertWithIdAtAsync(_app, NewLargerId(), member, entryId, firstExpense, earlier);
            await DuplicateAnswerRows.InsertWithIdAtAsync(_app, NewSmallerId(), member, entryId, secondExpense, earlier.AddMinutes(5));

            await MigrateToAsync(null);

            Assert.Equal(DuplicateAnswerRequests.SameAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, member, entryId, firstExpense));
            Assert.Null(await DuplicateAnswerRows.AnswerOfAsync(_app, member, entryId, secondExpense));
        }

        [Fact]
        public async Task Migrate_OverTwoMergesOfTheSameExpenseWhereTheEarlierEntryWasDeleted_KeepsTheMergeOfTheLiveEntry()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid member = group.Member.UserId;
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid deletedEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            Guid liveEntry = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PersonalEntryRequests.DeleteAsync(group.Member.Client, deletedEntry);
            await MigrateToAsync(LastMigrationBeforeTheIndexes);
            await DuplicateAnswerRows.InsertAsync(_app, member, deletedEntry, expenseId);
            await DuplicateAnswerRows.InsertAsync(_app, member, liveEntry, expenseId);

            await MigrateToAsync(null);

            Assert.Null(await DuplicateAnswerRows.AnswerOfAsync(_app, member, deletedEntry, expenseId));
            Assert.Equal(DuplicateAnswerRequests.SameAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, member, liveEntry, expenseId));
        }

        [Fact]
        public async Task Migrate_DownAndUpAgain_DropsAndRecreatesTheIndexesAndKeepsEveryRow()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, expenseId, DuplicateAnswerRequests.SameAnswer);
            string before = await DuplicateAnswerRows.SnapshotAsync(_app);

            await MigrateToAsync(LastMigrationBeforeTheIndexes);

            Assert.Empty(await SameIndexNamesAsync());
            Assert.Equal(before, await DuplicateAnswerRows.SnapshotAsync(_app));

            await MigrateToAsync(null);

            Assert.Equal(2, (await SameIndexNamesAsync()).Count);
            Assert.Equal(before, await DuplicateAnswerRows.SnapshotAsync(_app));
        }

        [Theory]
        [InlineData("ix_duplicate_answers_user_id_personal_entry_id_same", "(user_id, personal_entry_id)")]
        [InlineData("ix_duplicate_answers_user_id_expense_id_same", "(user_id, expense_id)")]
        public async Task Index_IsUniqueOnThePersonAndOneSideOfThePairForSameAnswersOnly(string indexName, string columns)
        {
            List<string?> definitions = await _app.QueryAsync(
                "SELECT indexdef FROM pg_indexes WHERE tablename = 'duplicate_answers' AND indexname = @name",
                new NpgsqlParameter("name", indexName));

            string definition = Assert.Single(definitions)!;
            Assert.Contains("CREATE UNIQUE INDEX", definition, StringComparison.Ordinal);
            Assert.Contains(columns, definition, StringComparison.Ordinal);
            Assert.Contains("answer = 'Same'", definition, StringComparison.Ordinal);
        }

        public ValueTask InitializeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            await MigrateToAsync(null);
        }

        private static Guid NewSmallerId()
        {
            return Guid.Parse("00000000-" + Guid.NewGuid().ToString()[9..]);
        }

        private static Guid NewLargerId()
        {
            return Guid.Parse("ffffffff-" + Guid.NewGuid().ToString()[9..]);
        }

        private async Task<List<string?>> SameIndexNamesAsync()
        {
            return await _app.QueryAsync(
                "SELECT indexname FROM pg_indexes WHERE tablename = 'duplicate_answers' AND indexname LIKE '%same' ORDER BY indexname");
        }

        private async Task MigrateToAsync(string? targetMigration)
        {
            using IServiceScope scope = _app.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await context.GetService<IMigrator>().MigrateAsync(targetMigration, TestContext.Current.CancellationToken);
        }
    }
}
