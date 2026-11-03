using System.Globalization;
using System.Net;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class AddExpenseLateNewRequestTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

        [Fact]
        public async Task Add_CategoryArchivedAfterTheExpenseWasTyped_Answers400CategoryInvalidAndWritesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid categoryId = await InsertCategoryAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId, group.MemberRowId) with { CategoryId = categoryId };
            await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await _app.ExecuteAsync("UPDATE categories SET archived_at = now() WHERE id = @id", new NpgsqlParameter("id", categoryId));
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_CATEGORY_INVALID);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Add_DateThatWasTheFarthestAllowedWhenTypedADayEarlier_IsStillAccepted()
        {
            _app.Clock.SetTo(ExpensesApp.ClockStart);
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            string farthestWhenTyped = _app.Clock.UtcToday.AddDays(366).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId) with { ExpenseDate = farthestWhenTyped };
            _app.Clock.Advance(OneDay);

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            Assert.Equal(farthestWhenTyped, await ExpenseRows.ExpenseValueAsync(_app, expenseId, "expense_date"));
        }

        [Fact]
        public async Task Add_DateOneDayPastTheLimitWhenTypedIsAcceptedOnceTheServerClockMovedOneDayForward()
        {
            _app.Clock.SetTo(ExpensesApp.ClockStart);
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            string pastTheLimitWhenTyped = _app.Clock.UtcToday.AddDays(367).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.OwnerRowId, group.OwnerRowId) with { ExpenseDate = pastTheLimitWhenTyped };
            HttpResponseMessage refused = await ExpenseRequests.AddAsync(group.Owner.Client, group.GroupId, input);
            await ProblemResponse.AssertAsync(refused, HttpStatusCode.BadRequest, ResultCodes.EXPENSE_DATE_INVALID);
            _app.Clock.Advance(OneDay);

            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            Assert.Equal(pastTheLimitWhenTyped, await ExpenseRows.ExpenseValueAsync(_app, expenseId, "expense_date"));
        }

        private async Task<Guid> InsertCategoryAsync()
        {
            Guid categoryId = Guid.CreateVersion7();
            await _app.ExecuteAsync(
                "INSERT INTO categories (id, owner_user_id, key, name, emoji, color, sort_order, archived_at) "
                + "VALUES (@id, NULL, @key, NULL, @emoji, @color, @sort_order, NULL)",
                new NpgsqlParameter("id", categoryId),
                new NpgsqlParameter("key", $"retiring-{categoryId:N}"),
                new NpgsqlParameter("emoji", "\U0001F43E"),
                new NpgsqlParameter("color", "slate"),
                new NpgsqlParameter("sort_order", 50));

            return categoryId;
        }
    }
}
