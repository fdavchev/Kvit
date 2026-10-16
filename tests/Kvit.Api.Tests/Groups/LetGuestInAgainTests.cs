using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Balances;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Kvit.Domain.Results;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Groups
{
    public class LetGuestInAgainTests(ExpensesApp _app) : IClassFixture<ExpensesApp>
    {
        private const string FreeName = "Marko";

        [Fact]
        public async Task LetInAgain_GuestWhoTappedAFreeName_Answers204AndTheSameRowIsAPlainNameAgain()
        {
            (ExpenseGroup group, GuestMember guest) = await CreateGroupWithGuestAsync();
            int rowsBefore = await GroupRows.CountMembersAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.LetGuestInAgainAsync(group.Owner.Client, group.GroupId, guest.RowId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, guest.RowId, "user_id"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, guest.RowId, "claimed_at"));
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, guest.RowId, "removed_at"));
            Assert.Equal(FreeName, await GroupRows.MemberRowValueAsync(_app, guest.RowId, "name"));
            Assert.Equal(rowsBefore, await GroupRows.CountMembersAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task LetInAgain_GuestWhoJoinedWithANewName_Answers204AndTheRowBecomesAPlainNameUnderThatName()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync();
            GuestMember guest = await GuestSteps.JoinWithNewNameAsync(_app, group.GroupId, "Cvetan");

            HttpResponseMessage response = await GroupRequests.LetGuestInAgainAsync(group.Owner.Client, group.GroupId, guest.RowId);

            await SettlementSteps.AssertNoContentAsync(response);
            Assert.Null(await GroupRows.MemberRowValueAsync(_app, guest.RowId, "user_id"));
            Assert.Equal("Cvetan", await GroupRows.MemberRowValueAsync(_app, guest.RowId, "name"));
        }

        [Fact]
        public async Task LetInAgain_ShowsTheRowAsAFreeNameInTheMembersListAndTheInvitePreview()
        {
            (ExpenseGroup group, GuestMember guest) = await CreateGroupWithGuestAsync();

            await SettlementSteps.AssertNoContentAsync(await GroupRequests.LetGuestInAgainAsync(group.Owner.Client, group.GroupId, guest.RowId));

            JsonElement row = await GroupRequests.MemberRowAsync(group.Owner.Client, group.GroupId, guest.RowId);
            Assert.True(row.GetProperty("isNameOnly").GetBoolean());
            Assert.False(row.GetProperty("isGuest").GetBoolean());
            Assert.Equal(JsonValueKind.Null, row.GetProperty("userId").ValueKind);
            JsonElement preview = await AuthRequests.ReadJsonAsync(await GroupRequests.PreviewAsync(_app.CreateClient(), await GuestSteps.InviteTokenOfAsync(_app, group.GroupId)));
            Assert.Contains(guest.RowId, GroupRequests.IdsOf(preview.GetProperty("unclaimedNames")));
        }

        [Fact]
        public async Task LetInAgain_LeavesTheOldGuestUserAloneButOutOfTheGroup()
        {
            (ExpenseGroup group, GuestMember guest) = await CreateGroupWithGuestAsync();

            await SettlementSteps.AssertNoContentAsync(await GroupRequests.LetGuestInAgainAsync(group.Owner.Client, group.GroupId, guest.RowId));

            List<string?> stored = await _app.QueryAsync("SELECT is_guest::text FROM users WHERE id = @id", new NpgsqlParameter("id", guest.UserId));
            Assert.Equal("true", Assert.Single(stored));
            Assert.Equal(HttpStatusCode.OK, (await AuthRequests.GetMeAsync(guest.Client)).StatusCode);
            await ProblemResponse.AssertAsync(await GroupRequests.GetAsync(guest.Client, group.GroupId), HttpStatusCode.NotFound, ResultCodes.GROUP_NOT_FOUND);
            Assert.Equal(0, await GroupRows.CountMemberRowsOfUserAsync(_app, group.GroupId, guest.UserId));
        }

        [Fact]
        public async Task LetInAgain_WritesOneGuestLetInAgainEventByTheOwnerAboutTheRowThatTheActivityShows()
        {
            (ExpenseGroup group, GuestMember guest) = await CreateGroupWithGuestAsync();

            await SettlementSteps.AssertNoContentAsync(await GroupRequests.LetGuestInAgainAsync(group.Owner.Client, group.GroupId, guest.RowId));

            Assert.Equal(1, await GroupRows.CountEventsOfTypeAsync(_app, group.GroupId, "GuestLetInAgain"));
            Assert.Equal(group.Owner.UserId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "GuestLetInAgain", "actor_user_id"));
            Assert.Equal(guest.RowId.ToString(), await GroupRows.EventValueAsync(_app, group.GroupId, "GuestLetInAgain", "member_id"));
            Assert.Equal(FreeName, await GroupRows.EventValueAsync(_app, group.GroupId, "GuestLetInAgain", "data->>'name'"));
            JsonElement events = await SettlementSteps.ReadActivityAsync(group.Owner, group.GroupId);
            Assert.Contains(events.EnumerateArray(), activityEvent => activityEvent.GetProperty("type").GetString() == "GuestLetInAgain");
        }

        [Fact]
        public async Task LetInAgain_ThenANewGuestTapsTheName_GetsTheSameRowWithItsExpenseAndItsBalance()
        {
            (ExpenseGroup group, GuestMember oldGuest) = await CreateGroupWithGuestAsync();
            Guid expenseId = await AddExpenseAsync(oldGuest, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, oldGuest.RowId, oldGuest.RowId, group.MemberRowId));
            await SettlementSteps.AssertNoContentAsync(await GroupRequests.LetGuestInAgainAsync(group.Owner.Client, group.GroupId, oldGuest.RowId));

            GuestMember newGuest = await GuestSteps.TapFreeNameAsync(_app, group.GroupId, oldGuest.RowId);

            Assert.NotEqual(oldGuest.UserId, newGuest.UserId);
            Assert.Equal(newGuest.UserId.ToString(), await GroupRows.MemberRowValueAsync(_app, oldGuest.RowId, "user_id"));
            Assert.Equal(oldGuest.RowId.ToString(), await ExpenseRows.ExpenseValueAsync(_app, expenseId, "paid_by_member_id"));
            JsonElement balances = BalanceJson.CurrencyOf(await BalanceRequests.ReadBalancedAsync(newGuest.Client, group.GroupId), ExpenseInputs.Mkd);
            JsonElement guestBalance = balances.GetProperty("balances").EnumerateArray().Single(row => row.GetProperty("memberId").GetGuid() == oldGuest.RowId);
            Assert.Equal(ExpenseInputs.OneThousandMkd / 2, guestBalance.GetProperty("balanceMinor").GetInt64());
        }

        [Fact]
        public async Task LetInAgain_AGuestWithAPendingPayment_KeepsThePendingPaymentOnTheSameRow()
        {
            (ExpenseGroup group, GuestMember oldGuest) = await CreateGroupWithGuestAsync();
            await AddExpenseAsync(oldGuest, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.MemberRowId, oldGuest.RowId, group.MemberRowId));
            Guid settlementId = await RecordPaymentAsync(oldGuest, group.GroupId, oldGuest.RowId, group.MemberRowId, ExpenseInputs.OneThousandMkd / 2);
            Assert.Equal("Pending", await SettlementRows.ValueAsync(_app, settlementId, "status"));

            await SettlementSteps.AssertNoContentAsync(await GroupRequests.LetGuestInAgainAsync(group.Owner.Client, group.GroupId, oldGuest.RowId));
            GuestMember newGuest = await GuestSteps.TapFreeNameAsync(_app, group.GroupId, oldGuest.RowId);

            Assert.Equal("Pending", await SettlementRows.ValueAsync(_app, settlementId, "status"));
            Assert.Equal(oldGuest.RowId.ToString(), await SettlementRows.ValueAsync(_app, settlementId, "from_member_id"));
            await SettlementSteps.ConfirmAsync(group.Member, group.GroupId, settlementId);
            await SettlementSteps.AssertAllBalancesZeroAsync(group.Member, group.GroupId);
            Assert.Equal(HttpStatusCode.OK, (await GroupRequests.GetAsync(newGuest.Client, group.GroupId)).StatusCode);
        }

        [Fact]
        public async Task LetInAgain_AGuestWhoStillOwesMoney_KeepsTheDebtOnTheRow()
        {
            (ExpenseGroup group, GuestMember guest) = await CreateGroupWithGuestAsync();
            await AddExpenseAsync(guest, group.GroupId, ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, group.MemberRowId, guest.RowId, group.MemberRowId));

            await SettlementSteps.AssertNoContentAsync(await GroupRequests.LetGuestInAgainAsync(group.Owner.Client, group.GroupId, guest.RowId));

            JsonElement balances = BalanceJson.CurrencyOf(await BalanceRequests.ReadBalancedAsync(group.Owner.Client, group.GroupId), ExpenseInputs.Mkd);
            JsonElement row = balances.GetProperty("balances").EnumerateArray().Single(balance => balance.GetProperty("memberId").GetGuid() == guest.RowId);
            Assert.Equal(-(ExpenseInputs.OneThousandMkd / 2), row.GetProperty("balanceMinor").GetInt64());
            Assert.True(row.GetProperty("isCurrent").GetBoolean());
        }

        [Fact]
        public async Task LetInAgain_RowOfAPersonWithAFullAccount_Answers400MemberNotGuestAndWritesNothing()
        {
            (ExpenseGroup group, _) = await CreateGroupWithGuestAsync();
            string before = await GroupRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.LetGuestInAgainAsync(group.Owner.Client, group.GroupId, group.MemberRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NOT_GUEST);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task LetInAgain_ThePlainNameOfNobody_Answers400MemberNotGuestAndWritesNothing()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync(FreeName);
            string before = await GroupRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.LetGuestInAgainAsync(group.Owner.Client, group.GroupId, group.PlainRowIds[0]);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NOT_GUEST);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task LetInAgain_TheOwnersOwnRow_Answers400MemberNotGuest()
        {
            (ExpenseGroup group, _) = await CreateGroupWithGuestAsync();

            HttpResponseMessage response = await GroupRequests.LetGuestInAgainAsync(group.Owner.Client, group.GroupId, group.OwnerRowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.BadRequest, ResultCodes.MEMBER_NOT_GUEST);
        }

        [Fact]
        public async Task LetInAgain_UnknownMemberId_Answers404MemberNotFound()
        {
            (ExpenseGroup group, _) = await CreateGroupWithGuestAsync();

            HttpResponseMessage response = await GroupRequests.LetGuestInAgainAsync(group.Owner.Client, group.GroupId, Guid.CreateVersion7());

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
        }

        [Fact]
        public async Task LetInAgain_AGuestWhoWasRemoved_Answers404MemberNotFoundAndWritesNothing()
        {
            (ExpenseGroup group, GuestMember guest) = await CreateGroupWithGuestAsync();
            await SettlementSteps.AssertNoContentAsync(await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, guest.RowId));
            string before = await GroupRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.LetGuestInAgainAsync(group.Owner.Client, group.GroupId, guest.RowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.NotFound, ResultCodes.MEMBER_NOT_FOUND);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, group.GroupId));
        }

        [Fact]
        public async Task LetInAgain_ByTheGuestThemselves_Answers403GroupNotOwnerAndWritesNothing()
        {
            (ExpenseGroup group, GuestMember guest) = await CreateGroupWithGuestAsync();
            string before = await GroupRows.SnapshotAsync(_app, group.GroupId);

            HttpResponseMessage response = await GroupRequests.LetGuestInAgainAsync(guest.Client, group.GroupId, guest.RowId);

            await ProblemResponse.AssertAsync(response, HttpStatusCode.Forbidden, ResultCodes.GROUP_NOT_OWNER);
            Assert.Equal(before, await GroupRows.SnapshotAsync(_app, group.GroupId));
        }

        private async Task<(ExpenseGroup Group, GuestMember Guest)> CreateGroupWithGuestAsync()
        {
            ExpenseGroup group = await _app.CreateExpenseGroupAsync(FreeName);
            GuestMember guest = await GuestSteps.TapFreeNameAsync(_app, group.GroupId, group.PlainRowIds[0]);

            return (group, guest);
        }

        private static async Task<Guid> AddExpenseAsync(GuestMember adder, Guid groupId, ExpenseInput input)
        {
            HttpResponseMessage response = await ExpenseRequests.AddAsync(adder.Client, groupId, input);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid();
        }

        private static async Task<Guid> RecordPaymentAsync(GuestMember recorder, Guid groupId, Guid fromMemberId, Guid toMemberId, long amountMinor)
        {
            HttpResponseMessage response = await SettlementRequests.RecordAsync(recorder.Client, groupId, fromMemberId, toMemberId, amountMinor, ExpenseInputs.Mkd);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid();
        }
    }
}
