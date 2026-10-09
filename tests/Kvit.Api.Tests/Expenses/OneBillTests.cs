using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Expenses
{
    public class OneBillTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string Pizza = "\U0001F355";

        [Fact]
        public async Task OneBill_FullRequest_Answers200WithExactlyGroupIdAndExpenseId()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = await FullInputAsync();

            HttpResponseMessage response = await ExpenseRequests.OneBillAsync(caller.Client, input);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            JsonElement body = await AuthRequests.ReadJsonAsync(response);
            Assert.Equal(["expenseId", "groupId"], GroupRequests.PropertyNamesOf(body));
            Assert.NotEqual(Guid.Empty, body.GetProperty("groupId").GetGuid());
            Assert.NotEqual(Guid.Empty, body.GetProperty("expenseId").GetGuid());
        }

        [Fact]
        public async Task OneBill_FullRequest_CreatesAGroupOfKindOneBillOwnedByTheCaller()
        {
            SignedInUser caller = await _app.SignUpAsync();

            (Guid groupId, Guid expenseId) = await _app.CreateOneBillAsync(caller, await FullInputAsync());

            Assert.Equal("OneBill", await GroupRows.GroupValueAsync(_app, groupId, "kind"));
            Assert.Equal("Dinner at Gradot", await GroupRows.GroupValueAsync(_app, groupId, "name"));
            Assert.Equal(Pizza, await GroupRows.GroupValueAsync(_app, groupId, "emoji"));
            Assert.Equal("MKD", await GroupRows.GroupValueAsync(_app, groupId, "default_currency"));
            Assert.Equal("Open", await GroupRows.GroupValueAsync(_app, groupId, "status"));
            Assert.Equal(caller.UserId.ToString(), await GroupRows.GroupValueAsync(_app, groupId, "owner_user_id"));
            Assert.Equal(caller.UserId.ToString(), await GroupRows.GroupValueAsync(_app, groupId, "created_by_user_id"));
            Assert.Null(await GroupRows.GroupValueAsync(_app, groupId, "deleted_at"));
            Assert.Equal(groupId.ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "group_id"));
            Assert.Matches(GroupRequests.TokenFormat, await GroupRows.GroupValueAsync(_app, groupId, "invite_token"));
        }

        [Fact]
        public async Task OneBill_FullRequest_CreatesTheCallersRowAndOnePlainNameRowPerName()
        {
            SignedInUser caller = await _app.SignUpAsync();

            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, await FullInputAsync());

            Assert.Equal(3, await GroupRows.CountMembersAsync(_app, groupId));
            Assert.Equal(GroupsApp.OwnerName, await GroupRows.MemberValueAsync(_app, groupId, caller.UserId, "name"));
            Assert.Null(await GroupRows.MemberValueAsync(_app, groupId, caller.UserId, "removed_at"));
            Guid marko = await _app.MemberRowIdByNameAsync(groupId, "Marko");
            Guid petar = await _app.MemberRowIdByNameAsync(groupId, "Petar");
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, marko, "user_id"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, petar, "user_id"));
            Assert.Equal(caller.UserId.ToString(), await GroupRows.MemberRowValueAsync(_app, marko, "added_by_user_id"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, marko, "removed_at"));
        }

        [Fact]
        public async Task OneBill_FullRequest_ListsThePeopleInTheOrderOfTheirIndexes()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko", "Petar", "Stefan", "Jovan");

            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, input);

            JsonElement members = (await GroupRequests.ReadMembersAsync(caller.Client, groupId)).GetProperty("members");
            Assert.Equal(["Ana", "Marko", "Petar", "Stefan", "Jovan"], [.. members.EnumerateArray().Select(member => member.GetProperty("name").GetString()!)]);
        }

        [Fact]
        public async Task OneBill_FullRequest_StoresTheExpenseWithThePayerByIndexAndTheShares()
        {
            SignedInUser caller = await _app.SignUpAsync();
            Guid food = await _app.BuiltInCategoryIdAsync("food");

            (Guid groupId, Guid expenseId) = await _app.CreateOneBillAsync(caller, await FullInputAsync());

            Guid marko = await _app.MemberRowIdByNameAsync(groupId, "Marko");
            Assert.Equal(marko.ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "paid_by_member_id"));
            Assert.Equal("Shares", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "split_type"));
            Assert.Equal("120000", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "amount_minor"));
            Assert.Equal("MKD", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "currency"));
            Assert.Equal("Birthday", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "note"));
            Assert.Equal(food.ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "category_id"));
            Assert.Equal("2026-10-05", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "expense_date"));
            Assert.Equal(caller.UserId.ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "created_by_user_id"));
            List<StoredShare> shares = await ExpenseRows.SharesOfAsync(_app, expenseId);
            Assert.Equal([60_000L, 30_000L, 30_000L], [.. shares.Select(share => share.ShareMinor)]);
            Assert.Equal([2L, 1L, 1L], [.. shares.Select(share => share.InputValue)]);
        }

        [Fact]
        public async Task OneBill_FullRequest_ShowsTheExpenseThroughTheExpenseEndpointsOfTheNewGroup()
        {
            SignedInUser caller = await _app.SignUpAsync();

            (Guid groupId, Guid expenseId) = await _app.CreateOneBillAsync(caller, await FullInputAsync());

            JsonElement detail = await _app.ReadExpenseAsync(caller, groupId, expenseId);
            Assert.Equal("Marko", detail.GetProperty("paidByName").GetString());
            Assert.Equal(["Ana", "Marko", "Petar"], ExpenseJson.ShareNames(detail));
            Assert.Equal([60_000L, 30_000L, 30_000L], ExpenseJson.ShareMinors(detail));
            Assert.Equal("Dinner at Gradot", detail.GetProperty("title").GetString());
        }

        [Fact]
        public async Task OneBill_FullRequest_ShowsTheGroupInTheGroupsListAndTheGroupAsOneBillOwnedByTheCaller()
        {
            SignedInUser caller = await _app.SignUpAsync();

            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, await FullInputAsync());

            JsonElement listed = await AuthRequests.ReadJsonAsync(await GroupRequests.ListAsync(caller.Client));
            JsonElement row = GroupRequests.RowWithId(listed.GetProperty("groups"), groupId);
            Assert.Equal("OneBill", row.GetProperty("kind").GetString());
            Assert.Equal(3, row.GetProperty("memberCount").GetInt32());
            JsonElement group = await AuthRequests.ReadJsonAsync(await GroupRequests.GetAsync(caller.Client, groupId));
            Assert.Equal("OneBill", group.GetProperty("kind").GetString());
            Assert.True(group.GetProperty("isOwner").GetBoolean());
            Assert.Equal(caller.UserId, group.GetProperty("ownerUserId").GetGuid());
        }

        [Fact]
        public async Task OneBill_FullRequest_WritesGroupCreatedMemberAddedForEachNameAndExpenseAddedEventsByTheCaller()
        {
            SignedInUser caller = await _app.SignUpAsync();

            (Guid groupId, Guid expenseId) = await _app.CreateOneBillAsync(caller, await FullInputAsync());

            Assert.Equal(4, await GroupRows.CountEventsAsync(_app, groupId));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, groupId, "GroupCreated"));
            Assert.Equal(2, await GroupRows.CountEventsOfTypeAsync(_app, groupId, "MemberAdded"));
            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, groupId, "ExpenseAdded"));
            Assert.Equal("Dinner at Gradot", await GroupRows.EventValueAsync(_app, groupId, "GroupCreated", "data->>'name'"));
            Assert.Equal(expenseId.ToString(), await GroupRows.EventValueAsync(_app, groupId, "ExpenseAdded", "expense_id"));
            Assert.Equal(caller.UserId.ToString(), await GroupRows.EventValueAsync(_app, groupId, "ExpenseAdded", "actor_user_id"));
            Assert.Equal(4, await ExpenseRows.CountEventsByActorAsync(_app, caller.UserId));
        }

        [Fact]
        public async Task OneBill_FullRequest_WritesAMemberAddedEventAboutEachPlainNameRow()
        {
            SignedInUser caller = await _app.SignUpAsync();

            (Guid groupId, _) = await _app.CreateOneBillAsync(caller, await FullInputAsync());

            Guid marko = await _app.MemberRowIdByNameAsync(groupId, "Marko");
            Guid petar = await _app.MemberRowIdByNameAsync(groupId, "Petar");
            List<string?> aboutMembers = await _app.QueryAsync(
                "SELECT member_id::text FROM activity_events WHERE group_id = @group_id AND type = 'MemberAdded'",
                new NpgsqlParameter("group_id", groupId));
            List<string> expectedMemberIds = [.. new[] { marko.ToString(), petar.ToString() }.Order(StringComparer.Ordinal)];
            List<string> actualMemberIds = [.. aboutMembers.OfType<string>().Order(StringComparer.Ordinal)];
            Assert.Equal(expectedMemberIds, actualMemberIds);
            List<string?> names = await _app.QueryAsync(
                "SELECT data->>'name' FROM activity_events WHERE group_id = @group_id AND type = 'MemberAdded'",
                new NpgsqlParameter("group_id", groupId));
            List<string> sortedNames = [.. names.OfType<string>().Order(StringComparer.Ordinal)];
            Assert.Equal(["Marko", "Petar"], sortedNames);
        }

        [Fact]
        public async Task OneBill_FullRequest_WritesAGroupCreatedAndAnExpenseAddedUsageEventForTheCaller()
        {
            SignedInUser caller = await _app.SignUpAsync();

            await _app.CreateOneBillAsync(caller, await FullInputAsync());

            Assert.Equal(1, await GroupRows.CountUsageEventsAsync(_app, caller.UserId, "GroupCreated"));
            Assert.Equal(1, await GroupRows.CountUsageEventsAsync(_app, caller.UserId, "ExpenseAdded"));
        }

        [Fact]
        public async Task OneBill_ExpenseWithoutAnyNames_CreatesAOnePersonBill()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd);

            (Guid groupId, Guid expenseId) = await _app.CreateOneBillAsync(caller, input);

            Assert.Equal(1, await GroupRows.CountMembersAsync(_app, groupId));
            Assert.Equal(0, await GroupRows.CountEventsOfTypeAsync(_app, groupId, "MemberAdded"));
            List<StoredShare> shares = await ExpenseRows.SharesOfAsync(_app, expenseId);
            Assert.Equal([ExpenseInputs.OneThousandMkd], [.. shares.Select(share => share.ShareMinor)]);
        }

        [Fact]
        public async Task OneBill_PayerNotInTheSplit_GivesTheLeftoverToTheFirstPersonInTheSplitByIndex()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = new(
                null,
                null,
                ["Marko", "Petar", "Stefan"],
                null,
                ExpenseInputs.OneThousandMkd,
                ExpenseInputs.Mkd,
                ExpenseInputs.DefaultDate,
                null,
                0,
                "Equal",
                [new OneBillShareInput(3, 0), new OneBillShareInput(2, 0), new OneBillShareInput(1, 0)]);

            (_, Guid expenseId) = await _app.CreateOneBillAsync(caller, input);

            List<StoredShare> shares = await ExpenseRows.SharesOfAsync(_app, expenseId);
            Assert.Equal([33_400L, 33_300L, 33_300L], [.. shares.Select(share => share.ShareMinor)]);
        }

        [Fact]
        public async Task OneBill_EurBill_StoresTheEurAmountAndMakesEurTheGroupsDefaultCurrency()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(1_000, ExpenseInputs.Eur, "Marko", "Petar");

            (Guid groupId, Guid expenseId) = await _app.CreateOneBillAsync(caller, input);

            Assert.Equal("EUR", await GroupRows.GroupValueAsync(_app, groupId, "default_currency"));
            List<StoredShare> shares = await ExpenseRows.SharesOfAsync(_app, expenseId);
            Assert.Equal([334L, 333L, 333L], [.. shares.Select(share => share.ShareMinor)]);
        }

        [Fact]
        public async Task OneBill_Expense_SavesTheCurrentExchangeRate()
        {
            await _app.PrepareRatesAsync(new DateOnly(2026, 10, 2), 61.5000m, TimeSpan.FromHours(1));
            SignedInUser caller = await _app.SignUpAsync();

            (_, Guid expenseId) = await _app.CreateOneBillAsync(caller, OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko"));

            Assert.Equal("61.5000", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "mkd_per_eur"));
            Assert.Equal("2026-10-02", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "rate_date"));
        }

        [Fact]
        public async Task OneBill_Expense_WhenTheRateIsStaleSavesTheFetchedRate()
        {
            await _app.PrepareRatesAsync(new DateOnly(2026, 10, 1), 61.5000m, TimeSpan.FromHours(9));
            _app.RateSource.AnswerWith(new DateOnly(2026, 10, 2), 61.8123m);
            SignedInUser caller = await _app.SignUpAsync();

            (_, Guid expenseId) = await _app.CreateOneBillAsync(caller, OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko"));

            Assert.Equal("61.8123", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "mkd_per_eur"));
            Assert.Equal("2026-10-02", await ExpenseRows.ExpenseValueAsync(_app, expenseId, "rate_date"));
        }

        [Fact]
        public async Task OneBill_TwoBillsOfTheSameCaller_AreTwoSeparateGroups()
        {
            SignedInUser caller = await _app.SignUpAsync();
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");

            (Guid firstGroup, _) = await _app.CreateOneBillAsync(caller, input);
            (Guid secondGroup, _) = await _app.CreateOneBillAsync(caller, input);

            Assert.NotEqual(firstGroup, secondGroup);
            Assert.Equal(2, await GroupRows.CountGroupsOwnedByAsync(_app, caller.UserId));
        }

        private async Task<OneBillInput> FullInputAsync()
        {
            Guid food = await _app.BuiltInCategoryIdAsync("food");

            return new OneBillInput(
                "Dinner at Gradot",
                Pizza,
                ["Marko", "Petar"],
                "Birthday",
                ExpenseInputs.TwelveHundredMkd,
                ExpenseInputs.Mkd,
                ExpenseInputs.DefaultDate,
                food,
                1,
                "Shares",
                [new OneBillShareInput(0, 2), new OneBillShareInput(1, 1), new OneBillShareInput(2, 1)]);
        }
    }
}
