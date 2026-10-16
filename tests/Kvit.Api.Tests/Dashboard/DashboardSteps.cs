using System.Net;
using System.Text.Json;
using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Balances;
using Kvit.Api.Tests.Closing;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Npgsql;
using Xunit;

namespace Kvit.Api.Tests.Dashboard
{
    public static class DashboardSteps
    {
        public const long ExpenseMinor = 60_000;
        public const long ShareMinor = 30_000;

        public static async Task<ExpenseGroup> CreateGroupAsync(ExpensesApp app, SignedInUser owner, SignedInUser member, string name = GroupRows.DefaultName)
        {
            app.Clock.Advance(ClosingSteps.OneMinute);
            HttpResponseMessage response = await GroupRequests.CreateAsync(owner.Client, name);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Guid groupId = (await AuthRequests.ReadJsonAsync(response)).GetProperty("id").GetGuid();
            Guid ownerRowId = await GroupRows.CurrentMemberIdAsync(app, groupId, owner.UserId);
            Guid memberRowId = await GroupRows.InsertMemberAsync(app, groupId, member.UserId, member.Form.DisplayName, owner.UserId, joinedAt: app.Clock.GetUtcNow());

            return new ExpenseGroup(groupId, owner, ownerRowId, member, memberRowId, []);
        }

        public static async Task<ExpenseGroup> CreateGroupWithNewMemberAsync(ExpensesApp app, SignedInUser owner, string name = GroupRows.DefaultName)
        {
            SignedInUser member = await app.SignUpAsync(GroupsApp.MemberName);

            return await CreateGroupAsync(app, owner, member, name);
        }

        public static async Task<ExpenseGroup> CreateClosingGroupAsync(ExpensesApp app, SignedInUser owner, string name = GroupRows.DefaultName)
        {
            ExpenseGroup group = await CreateGroupWithNewMemberAsync(app, owner, name);
            await ClosingSteps.AddCountedExpenseAsync(app, group.GroupId, owner, group.OwnerRowId);
            await ClosingSteps.StartAsync(owner, group.GroupId);

            return group;
        }

        public static async Task<Guid> CreateOneBillWithExpenseAsync(ExpensesApp app, SignedInUser owner)
        {
            app.Clock.Advance(ClosingSteps.OneMinute);
            OneBillInput input = OneBillInput.EqualAmongEveryone(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, "Marko");
            (Guid groupId, _) = await app.CreateOneBillAsync(owner, input);

            return groupId;
        }

        public static async Task<Guid> CreateEmptyOneBillJoinedNowAsync(ExpensesApp app, SignedInUser owner)
        {
            app.Clock.Advance(ClosingSteps.OneMinute);
            Guid groupId = await GroupRows.InsertGroupAsync(app, owner.UserId, kind: "OneBill", name: "Bill");
            await GroupRows.InsertMemberAsync(app, groupId, owner.UserId, GroupRows.OwnerMemberName, owner.UserId, joinedAt: app.Clock.GetUtcNow());

            return groupId;
        }

        public static Task<Guid> OwnerPaysForBothAsync(ExpensesApp app, ExpenseGroup group, string currency = ExpenseInputs.Mkd)
        {
            return AddExpenseLaterAsync(app, group.GroupId, group.Owner, ExpenseInputs.Equal(ExpenseMinor, currency, group.OwnerRowId, group.OwnerRowId, group.MemberRowId));
        }

        public static Task<Guid> MemberPaysForBothAsync(ExpensesApp app, ExpenseGroup group, string currency = ExpenseInputs.Mkd)
        {
            return AddExpenseLaterAsync(app, group.GroupId, group.Member, ExpenseInputs.Equal(ExpenseMinor, currency, group.MemberRowId, group.OwnerRowId, group.MemberRowId));
        }

        public static Task<Guid> AddOwnExpenseAsync(ExpensesApp app, Guid groupId, SignedInUser adder, Guid adderRowId, string? title = null)
        {
            ExpenseInput input = ExpenseInputs.Equal(ExpenseInputs.OneThousandMkd, ExpenseInputs.Mkd, adderRowId, adderRowId) with { Title = title };

            return AddExpenseLaterAsync(app, groupId, adder, input);
        }

        public static async Task<long> BalanceOfAsync(SignedInUser viewer, Guid groupId, Guid memberRowId, string currency)
        {
            JsonElement body = await BalanceRequests.ReadBalancedAsync(viewer.Client, groupId);
            JsonElement currencyEntry = BalanceJson.CurrencyOf(body, currency);
            JsonElement row = currencyEntry.GetProperty("balances").EnumerateArray().Single(balance => balance.GetProperty("memberId").GetGuid() == memberRowId);

            return row.GetProperty("balanceMinor").GetInt64();
        }

        public static Task MarkClosingAsync(ExpensesApp app, Guid groupId)
        {
            return app.ExecuteAsync(
                "UPDATE groups SET status = 'Closing', closing_started_at = @started WHERE id = @id",
                new NpgsqlParameter("id", groupId),
                new NpgsqlParameter("started", app.Clock.GetUtcNow()));
        }

        public static Task MarkFinishedAsync(ExpensesApp app, Guid groupId)
        {
            return app.ExecuteAsync(
                "UPDATE groups SET status = 'Finished', finished_at = @finished WHERE id = @id",
                new NpgsqlParameter("id", groupId),
                new NpgsqlParameter("finished", app.Clock.GetUtcNow()));
        }

        public static void AdvanceTo(AdjustableClock clock, DateTimeOffset moment)
        {
            Assert.True(moment >= clock.GetUtcNow(), $"The test clock is at {clock.GetUtcNow():O}, already past {moment:O}.");
            clock.Advance(moment - clock.GetUtcNow());
        }

        private static async Task<Guid> AddExpenseLaterAsync(ExpensesApp app, Guid groupId, SignedInUser adder, ExpenseInput input)
        {
            app.Clock.Advance(ClosingSteps.OneMinute);

            return await app.AddExpenseAsync(adder, groupId, input);
        }
    }
}
