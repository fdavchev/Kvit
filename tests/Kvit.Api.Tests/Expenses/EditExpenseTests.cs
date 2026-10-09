using System.Globalization;
using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.Results;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class EditExpenseTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

        private static readonly Dictionary<string, (Func<ExpenseInput, ExpenseInput> Break, string ErrorCode)> BrokenInputs = new()
        {
            ["currency"] = (input => input with { Currency = "USD" }, ResultCodes.EXPENSE_CURRENCY_INVALID),
            ["splitType"] = (input => input with { SplitType = "Unknown" }, ResultCodes.EXPENSE_SPLIT_TYPE_INVALID),
            ["amountZero"] = (input => input with { AmountMinor = 0 }, ResultCodes.EXPENSE_AMOUNT_NOT_POSITIVE),
            ["amountTooLarge"] = (input => input with { AmountMinor = 1_000_000_000_000 }, ResultCodes.EXPENSE_AMOUNT_TOO_LARGE),
            ["amountOffTheStep"] = (input => input with { AmountMinor = 150 }, ResultCodes.MONEY_NOT_ON_CURRENCY_STEP),
            ["dateTooEarly"] = (input => input with { ExpenseDate = "1999-12-31" }, ResultCodes.EXPENSE_DATE_INVALID),
            ["titleTooLong"] = (input => input with { Title = ExpenseInputs.TextOfLength(81) }, ResultCodes.EXPENSE_TITLE_INVALID),
            ["noteTooLong"] = (input => input with { Note = ExpenseInputs.TextOfLength(501) }, ResultCodes.EXPENSE_NOTE_INVALID),
            ["categoryUnknown"] = (input => input with { CategoryId = Guid.CreateVersion7() }, ResultCodes.EXPENSE_CATEGORY_INVALID),
            ["noShares"] = (input => input with { Shares = [] }, ResultCodes.EXPENSE_SPLIT_NO_PARTICIPANTS),
        };

        public static TheoryData<string> BrokenInputNames => [.. BrokenInputs.Keys];

        [Fact]
        public async Task Edit_ByWhoeverAddedIt_Answers204WithNoBody()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.MemberRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, input);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Member.Client, group.GroupId, expenseId, input with { Title = "Lunch" });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Edit_ByTheOwnerOfAnExpenseAddedByAnotherMember_Answers204AndTheCreatorStaysTheSame()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.MemberRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, input);
            string createdAt = (await ExpenseRows.ExpenseValueAsync(_app, expenseId, "created_at"))!;
            _app.Clock.Advance(OneMinute);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { Title = "Lunch" });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(group.Member.UserId.ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "created_by_user_id"));
            Assert.Equal(createdAt, await ExpenseRows.ExpenseValueAsync(_app, expenseId, "created_at"));
            Assert.Equal(group.Owner.UserId.ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "updated_by_user_id"));
        }

        [Fact]
        public async Task Edit_ByAMemberWhoDidNotAddItAndIsNotTheOwner_Answers403ExpenseNotAllowedAndChangesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Member.Client, group.GroupId, expenseId, input with { Title = "Lunch" });

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.EXPENSE_NOT_ALLOWED);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Edit_EveryField_StoresTheNewValues()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            Guid groceries = await _app.BuiltInCategoryIdAsync("groceries");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId) with { Title = "Dinner", Note = "Lake" };
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            ExpenseInput changed = ExpenseInputs.Equal(2_000, "EUR", group.PlainRowIds[0], group.MemberRowId, group.PlainRowIds[0]) with
            {
                Title = "Lunch",
                Note = "Sea",
                ExpenseDate = "2026-10-04",
                CategoryId = groceries,
            };

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, changed);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("Lunch", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "title"));
            Assert.Equal("Sea", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "note"));
            Assert.Equal("2000", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "amount_minor"));
            Assert.Equal("EUR", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "currency"));
            Assert.Equal("2026-10-04", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "expense_date"));
            Assert.Equal(groceries.ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "category_id"));
            Assert.Equal(group.PlainRowIds[0].ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "paid_by_member_id"));
        }

        [Fact]
        public async Task Edit_CategoryRemoved_StoresNull()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid food = await _app.BuiltInCategoryIdAsync("food");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId) with { CategoryId = food };
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { CategoryId = null });

            Assert.Null(await ExpenseRows.ExpenseValueAsync(_app, expenseId, "category_id"));
        }

        [Fact]
        public async Task Edit_SplitAmongFewerPeople_ReplacesTheSharesWithTheRecalculatedOnes()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, [.. group.RowIdsInJoiningOrder]);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.MemberRowId, group.PlainRowIds[0]));

            List<StoredShare> stored = await ExpenseRows.SharesOfAsync(_app, expenseId);
            Assert.Equal([group.MemberRowId, group.PlainRowIds[0]], [.. stored.Select(share => share.MemberId)]);
            Assert.Equal([50_000L, 50_000L], [.. stored.Select(share => share.ShareMinor)]);
        }

        [Fact]
        public async Task Edit_SplitTypeChangedToExact_StoresTheTypedAmountsAndTheNewType()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            ExpenseInput exact = ExpenseInputs.Of("Exact", ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, new ShareInput(group.OwnerRowId, 70_000), new ShareInput(group.MemberRowId, 30_000));

            await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, exact);

            Assert.Equal("Exact", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "split_type"));
            List<StoredShare> stored = await ExpenseRows.SharesOfAsync(_app, expenseId);
            Assert.Equal([70_000L, 30_000L], [.. stored.Select(share => share.ShareMinor)]);
            Assert.Equal([70_000L, 30_000L], [.. stored.Select(share => share.InputValue)]);
        }

        [Fact]
        public async Task Edit_PayerChanged_MovesTheLeftoverToTheNewPayer()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, [.. group.RowIdsInJoiningOrder]);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { PaidByMemberId = group.PlainRowIds[0] });

            JsonElement detail = await _app.ReadExpenseAsync(group.Owner, group.GroupId, expenseId);
            Assert.Equal([33_300L, 33_300L, 33_400L], ExpenseJson.ShareMinors(detail));
        }

        [Fact]
        public async Task Edit_Changed_SetsUpdatedAtFromTheClockAndUpdatedByToTheEditor()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.MemberRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, input);
            _app.Clock.Advance(OneMinute);
            string expectedUpdatedAt = _app.Clock.GetUtcNow().UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            await ExpenseRequests.EditAsync(group.Member.Client, group.GroupId, expenseId, input with { Title = "Lunch" });

            Assert.Equal(expectedUpdatedAt, await ExpenseRows.ExpenseValueAsync(_app, expenseId, "updated_at AT TIME ZONE 'UTC'"));
            Assert.Equal(group.Member.UserId.ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "updated_by_user_id"));
        }

        [Fact]
        public async Task Edit_Changed_KeepsTheClientRequestIdOfTheExpense()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId);
            Guid clientRequestId = Guid.CreateVersion7();
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input, clientRequestId);

            await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { Title = "Lunch" });

            Assert.Equal(clientRequestId.ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "client_request_id"));
        }

        [Fact]
        public async Task Edit_Changed_WritesOneExpenseEditedEventByTheEditorAndNoUsageEvent()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, input);

            await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { Title = "Lunch" });

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ExpenseEdited"));
            Assert.Equal(group.Owner.UserId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "ExpenseEdited", "actor_user_id"));
            Assert.Equal(expenseId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "ExpenseEdited", "expense_id"));
            Assert.Equal(1, await GroupRows.CountUsageEventsAsync(_app, group.Member.UserId, "ExpenseAdded"));
            Assert.Equal(0, await GroupRows.CountUsageEventsAsync(_app, group.Owner.UserId, "ExpenseAdded"));
        }

        [Fact]
        public async Task Edit_NothingChanged_Answers204WritesNoEventAndLeavesUpdatedAtEmpty()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId) with { Title = "Dinner", Note = "Lake" };
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);
            _app.Clock.Advance(OneMinute);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ExpenseEdited"));
            Assert.Null(await ExpenseRows.ExpenseValueAsync(_app, expenseId, "updated_at"));
            Assert.Null(await ExpenseRows.ExpenseValueAsync(_app, expenseId, "updated_by_user_id"));
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Edit_NothingChangedAfterAnEarlierEdit_LeavesUpdatedAtAndUpdatedByAsTheyWere()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Member, group.GroupId, input);
            ExpenseInput edited = input with { Title = "Lunch" };
            _app.Clock.Advance(OneMinute);
            await ExpenseRequests.EditAsync(group.Member.Client, group.GroupId, expenseId, edited);
            string updatedAt = (await ExpenseRows.ExpenseValueAsync(_app, expenseId, "updated_at"))!;
            _app.Clock.Advance(OneMinute);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, edited);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(updatedAt, await ExpenseRows.ExpenseValueAsync(_app, expenseId, "updated_at"));
            Assert.Equal(group.Member.UserId.ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "updated_by_user_id"));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ExpenseEdited"));
        }

        [Fact]
        public async Task Edit_TitleThatOnlyDiffersBySurroundingSpaces_CountsAsNoChange()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId) with { Title = "Dinner" };
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { Title = "  Dinner  " });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "ExpenseEdited"));
            Assert.Null(await ExpenseRows.ExpenseValueAsync(_app, expenseId, "updated_at"));
        }

        [Theory]
        [MemberData(nameof(BrokenInputNames))]
        public async Task Edit_WithAnInvalidInput_Answers400WithTheCodeOfThatInputAndChangesNothing(string brokenInputName)
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);
            (Func<ExpenseInput, ExpenseInput> breakInput, string errorCode) = BrokenInputs[brokenInputName];

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, breakInput(input));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, errorCode);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Edit_ByAMemberWhoMayNotEditWithAnInvalidBody_Answers403BeforeLookingAtTheInput()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Member.Client, group.GroupId, expenseId, input with { Currency = "USD" });

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.EXPENSE_NOT_ALLOWED);
        }

        [Fact]
        public async Task Edit_ExpenseThatDoesNotExistWithAnInvalidBody_Answers404ExpenseNotFoundBeforeLookingAtTheInput()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "USD", group.OwnerRowId, group.OwnerRowId);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, Guid.CreateVersion7(), input);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.EXPENSE_NOT_FOUND);
        }

        [Fact]
        public async Task Edit_DeletedExpense_Answers404ExpenseNotFound()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ExpenseRequests.DeleteAsync(group.Owner.Client, group.GroupId, expenseId);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { Title = "Lunch" });

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.EXPENSE_NOT_FOUND);
        }

        [Fact]
        public async Task Edit_ExpenseOfAnotherGroupOfTheSameCaller_Answers404ExpenseNotFound()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            Guid otherGroupId = await _app.CreateGroupAsync(group.Owner);
            Guid ownerRowInOther = await GroupRows.CurrentMemberIdAsync(_app, otherGroupId, group.Owner.UserId);
            ExpenseInput inOther = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", ownerRowInOther, ownerRowInOther);
            Guid expenseInOther = await _app.AddExpenseAsync(group.Owner, otherGroupId, inOther);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseInOther, inOther with { Title = "Lunch" });

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.EXPENSE_NOT_FOUND);
            Assert.Null(await ExpenseRows.ExpenseValueAsync(_app, expenseInOther, "title"));
        }

        [Fact]
        public async Task Edit_PayerThatIsNotAMember_Answers404MemberNotFoundAndChangesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, input with { PaidByMemberId = Guid.CreateVersion7() });

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task Edit_ShareMemberThatWasRemoved_Answers404MemberNotFoundAndChangesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync("Marko");
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.MemberRowId);
            Guid expenseId = await _app.AddExpenseAsync(group.Owner, group.GroupId, input);
            await ExpenseRows.EndMemberRowAsync(_app, group.PlainRowIds[0], group.Owner.UserId);
            string before = await ExpenseRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await ExpenseRequests.EditAsync(group.Owner.Client, group.GroupId, expenseId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, "MKD", group.OwnerRowId, group.OwnerRowId, group.PlainRowIds[0]));

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(before, await ExpenseRows.SnapshotAsync(_app, group.GroupId));
        }
    }
}
