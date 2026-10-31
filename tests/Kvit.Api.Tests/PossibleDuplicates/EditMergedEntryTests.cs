using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.BudgetLimits;
using Kvit.Api.Tests.BudgetShares;
using Kvit.Api.Tests.BudgetSummary;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.PersonalEntries;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.PossibleDuplicates
{
    public class EditMergedEntryTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const int Repetitions = 10;
        private const long ChangedAmount = 54_000;
        private const string OriginalNote = "Lunch";
        private const decimal CurrentRate = 62.2500m;

        public static TheoryData<string> FieldsThatUnmerge => ["amount", "currency", "category", "date"];

        [Theory]
        [MemberData(nameof(FieldsThatUnmerge))]
        public async Task Edit_ChangingAnAmountCurrencyCategoryOrDate_RemovesTheSameAnswerAndAnswersNotMerged(string field)
        {
            MergedScene scene = await StartSceneAsync();
            await _app.PrepareRatesAsync(ScriptedExchangeRateSource.DefaultRateDate, CurrentRate, TimeSpan.FromHours(1));

            HttpResponseMessage response = await PersonalEntryRequests.EditAsync(scene.Group.Member.Client, scene.EntryId, ChangedInput(field));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(JsonValueKind.Null, body.GetProperty("mergedWithExpenseId").ValueKind);
            Assert.Null(await DuplicateAnswerRows.AnswerOfAsync(_app, scene.Group.Member.UserId, scene.EntryId, scene.ExpenseId));
            Assert.Null(await PossibleDuplicateSteps.MergedWithAsync(scene.Group.Member.Client, scene.EntryId));
        }

        [Fact]
        public async Task Edit_ChangingTheAmountOfAMergedEntry_MakesItCountInTheMonthTotalsAgain()
        {
            MergedScene scene = await StartSceneAsync();
            Assert.Equal(0, await PossibleDuplicateSteps.MonthSpendingTotalAsync(scene.Group.Member.Client));

            await PersonalEntryRequests.EditAsync(scene.Group.Member.Client, scene.EntryId, ChangedInput("amount"));

            Assert.Equal(ChangedAmount, await PossibleDuplicateSteps.MonthSpendingTotalAsync(scene.Group.Member.Client));
        }

        [Fact]
        public async Task Edit_ChangingTheAmountOfAMergedEntry_MakesItCountInTheLimitsAndTheSummaryAgain()
        {
            MergedScene scene = await StartSceneAsync();
            await BudgetLimitSteps.SetCategoryAsync(scene.Group.Member, PossibleDuplicateSteps.Food, 60_000);
            JsonElement limitsBefore = await BudgetLimitSteps.LimitsAsync(scene.Group.Member.Client);
            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(limitsBefore, PossibleDuplicateSteps.Food), PossibleDuplicateSteps.Food, 60_000, 50_000, 0, 0);

            await PersonalEntryRequests.EditAsync(scene.Group.Member.Client, scene.EntryId, ChangedInput("amount"));

            JsonElement limitsAfter = await BudgetLimitSteps.LimitsAsync(scene.Group.Member.Client);
            BudgetLimitSteps.AssertRow(BudgetLimitSteps.CategoryRowOf(limitsAfter, PossibleDuplicateSteps.Food), PossibleDuplicateSteps.Food, 60_000, 104_000, 44_000, 0);
            JsonElement summary = await BudgetSummarySteps.SummaryAsync(scene.Group.Member.Client);
            BudgetSummarySteps.AssertCurrencyRow(summary, PersonalEntryRequests.Mkd, 104_000, 0, 0, 0);
        }

        [Fact]
        public async Task Edit_ChangingOnlyTheNote_KeepsTheMerge()
        {
            MergedScene scene = await StartSceneAsync();

            HttpResponseMessage response = await PersonalEntryRequests.EditAsync(scene.Group.Member.Client, scene.EntryId, scene.Original with { Note = "Dinner" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(scene.ExpenseId, body.GetProperty("mergedWithExpenseId").GetGuid());
            Assert.Equal(DuplicateAnswerRequests.SameAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, scene.Group.Member.UserId, scene.EntryId, scene.ExpenseId));
            Assert.Equal(0, await PossibleDuplicateSteps.MonthSpendingTotalAsync(scene.Group.Member.Client));
        }

        [Fact]
        public async Task Edit_RemovingTheNote_KeepsTheMerge()
        {
            MergedScene scene = await StartSceneAsync();

            await PersonalEntryRequests.EditAsync(scene.Group.Member.Client, scene.EntryId, scene.Original with { Note = null });

            Assert.Equal(scene.ExpenseId, await PossibleDuplicateSteps.MergedWithAsync(scene.Group.Member.Client, scene.EntryId));
        }

        [Fact]
        public async Task Edit_SendingTheSameValuesAgain_KeepsTheMerge()
        {
            MergedScene scene = await StartSceneAsync();

            HttpResponseMessage response = await PersonalEntryRequests.EditAsync(scene.Group.Member.Client, scene.EntryId, scene.Original);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(scene.ExpenseId, await PossibleDuplicateSteps.MergedWithAsync(scene.Group.Member.Client, scene.EntryId));
            Assert.Equal(1, await DuplicateAnswerRows.CountOfUserAsync(_app, scene.Group.Member.UserId));
        }

        [Fact]
        public async Task Edit_ChangingTheAmountOfAnEntryWithOnlyNewAnswers_KeepsTheNewAnswers()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member, note: OriginalNote);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, expenseId, DuplicateAnswerRequests.NewAnswer);

            HttpResponseMessage response = await PersonalEntryRequests.EditAsync(group.Member.Client, entryId, Original() with { AmountMinor = ChangedAmount });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(DuplicateAnswerRequests.NewAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, group.Member.UserId, entryId, expenseId));
            Assert.Empty(await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Edit_ChangingTheAmountOfAMergedEntry_KeepsItsNewAnswersAndBringsTheMergedPairBack()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid mergedExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid notTheSameExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, group, "2026-10-04");
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member, note: OriginalNote);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, notTheSameExpense, DuplicateAnswerRequests.NewAnswer);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, mergedExpense, DuplicateAnswerRequests.SameAnswer);

            await PersonalEntryRequests.EditAsync(group.Member.Client, entryId, Original() with { AmountMinor = ChangedAmount });

            Assert.Equal(DuplicateAnswerRequests.NewAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, group.Member.UserId, entryId, notTheSameExpense));
            Assert.Null(await DuplicateAnswerRows.AnswerOfAsync(_app, group.Member.UserId, entryId, mergedExpense));
            Assert.Equal(PossibleDuplicateSteps.Keys((entryId, mergedExpense)), await PossibleDuplicateSteps.PairKeysAsync(group.Member.Client));
        }

        [Fact]
        public async Task Edit_ChangingTheAmountOfAMergedEntry_AlsoRemovesAStaleSameAnswerOfTheEntry()
        {
            MergedScene scene = await StartSceneAsync();
            Guid deletedExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, scene.Group, "2026-10-04");
            await DuplicateAnswerRows.InsertAsync(_app, scene.Group.Member.UserId, scene.EntryId, deletedExpense);
            await ExpenseRequests.DeleteAsync(scene.Group.Owner.Client, scene.Group.GroupId, deletedExpense);

            await PersonalEntryRequests.EditAsync(scene.Group.Member.Client, scene.EntryId, ChangedInput("amount"));

            Assert.Equal(0, await DuplicateAnswerRows.CountOfUserAsync(_app, scene.Group.Member.UserId));
        }

        [Fact]
        public async Task Edit_ChangingTheAmountOfOneEntry_LeavesTheMergeOfAnotherEntryOfTheSamePerson()
        {
            MergedScene scene = await StartSceneAsync();
            Guid otherExpense = await BudgetShareSteps.AddSharedExpenseAsync(_app, scene.Group, "2026-10-04");
            Guid otherEntry = await PossibleDuplicateSteps.AddEntryAsync(scene.Group.Member, entryDate: "2026-10-04");
            await PossibleDuplicateSteps.AnswerAsync(scene.Group.Member, otherEntry, otherExpense, DuplicateAnswerRequests.SameAnswer);

            await PersonalEntryRequests.EditAsync(scene.Group.Member.Client, scene.EntryId, ChangedInput("amount"));

            Assert.Equal(otherExpense, await PossibleDuplicateSteps.MergedWithAsync(scene.Group.Member.Client, otherEntry));
            Assert.Equal(DuplicateAnswerRequests.SameAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, scene.Group.Member.UserId, otherEntry, otherExpense));
        }

        [Fact]
        public async Task Edit_ChangingTheAmountOfMyEntry_LeavesTheMergeOfAnotherPersonOnTheSameExpense()
        {
            MergedScene scene = await StartSceneAsync();
            Guid ownerEntry = await PossibleDuplicateSteps.AddEntryAsync(scene.Group.Owner);
            await PossibleDuplicateSteps.AnswerAsync(scene.Group.Owner, ownerEntry, scene.ExpenseId, DuplicateAnswerRequests.SameAnswer);

            await PersonalEntryRequests.EditAsync(scene.Group.Member.Client, scene.EntryId, ChangedInput("amount"));

            Assert.Equal(scene.ExpenseId, await PossibleDuplicateSteps.MergedWithAsync(scene.Group.Owner.Client, ownerEntry));
            Assert.Equal(DuplicateAnswerRequests.SameAnswer, await DuplicateAnswerRows.AnswerOfAsync(_app, scene.Group.Owner.UserId, ownerEntry, scene.ExpenseId));
        }

        [Fact]
        public async Task Edit_WithAnInvalidDate_Answers400AndKeepsTheMerge()
        {
            MergedScene scene = await StartSceneAsync();
            string before = await DuplicateAnswerRows.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.EditAsync(scene.Group.Member.Client, scene.EntryId, scene.Original with { AmountMinor = ChangedAmount, EntryDate = "1999-12-31" });

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_DATE_INVALID);
            Assert.Equal(before, await DuplicateAnswerRows.SnapshotAsync(_app));
            Assert.Equal(scene.ExpenseId, await PossibleDuplicateSteps.MergedWithAsync(scene.Group.Member.Client, scene.EntryId));
        }

        [Fact]
        public async Task Edit_WithACategoryThePersonCannotUse_Answers400AndKeepsTheMerge()
        {
            MergedScene scene = await StartSceneAsync();
            string before = await DuplicateAnswerRows.SnapshotAsync(_app);

            HttpResponseMessage response = await PersonalEntryRequests.EditAsync(scene.Group.Member.Client, scene.EntryId, scene.Original with { AmountMinor = ChangedAmount, CategoryId = Guid.CreateVersion7() });

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.PERSONAL_ENTRY_CATEGORY_INVALID);
            Assert.Equal(before, await DuplicateAnswerRows.SnapshotAsync(_app));
            Assert.Equal(scene.ExpenseId, await PossibleDuplicateSteps.MergedWithAsync(scene.Group.Member.Client, scene.EntryId));
        }

        [Fact]
        public async Task Edit_TwoChangesOfAMergedEntryAtTheSameMoment_BothAnswer200AndTheSameAnswerIsGone()
        {
            MergedScene scene = await StartSceneAsync();

            for (int repetition = 0; repetition < Repetitions; repetition++)
            {
                await PossibleDuplicateSteps.AnswerAsync(scene.Group.Member, scene.EntryId, scene.ExpenseId, DuplicateAnswerRequests.SameAnswer);
                EntryInput first = scene.Original with { AmountMinor = 51_000 + repetition * 100 };
                EntryInput second = scene.Original with { AmountMinor = 52_000 + repetition * 100 };

                HttpResponseMessage[] responses = await Task.WhenAll(
                    PersonalEntryRequests.EditAsync(scene.Group.Member.Client, scene.EntryId, first),
                    PersonalEntryRequests.EditAsync(scene.Group.Member.Client, scene.EntryId, second));

                Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
                Assert.Null(await DuplicateAnswerRows.AnswerOfAsync(_app, scene.Group.Member.UserId, scene.EntryId, scene.ExpenseId));
            }
        }

        private static EntryInput Original()
        {
            return EntryInput.Of(PossibleDuplicateSteps.Food, PossibleDuplicateSteps.ShareMinor, PersonalEntryRequests.Mkd, ExpenseInputs.DefaultDate, OriginalNote);
        }

        private EntryInput ChangedInput(string field)
        {
            EntryInput original = Original();

            return field switch
            {
                "amount" => original with { AmountMinor = ChangedAmount },
                "currency" => original with { Currency = PersonalEntryRequests.Eur },
                "category" => original with { CategoryId = PossibleDuplicateSteps.Transport },
                "date" => original with { EntryDate = "2026-10-04" },
                _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Not a field of a personal entry."),
            };
        }

        private async Task<MergedScene> StartSceneAsync()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid expenseId = await BudgetShareSteps.AddSharedExpenseAsync(_app, group);
            Guid entryId = await PossibleDuplicateSteps.AddEntryAsync(group.Member, note: OriginalNote);
            await PossibleDuplicateSteps.AnswerAsync(group.Member, entryId, expenseId, DuplicateAnswerRequests.SameAnswer);

            return new MergedScene(group, expenseId, entryId, Original());
        }

        private sealed record MergedScene(ExpenseGroup Group, Guid ExpenseId, Guid EntryId, EntryInput Original);
    }
}
