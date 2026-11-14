using System.Net;
using Kvit.Api.Tests.BudgetCurrency;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Persistence;
using Kvit.Api.Tests.Settlements;
using Xunit;

namespace Kvit.Api.Tests.Export
{
    public static class ExportLeftSteps
    {
        public const string GroupName = "Weekend by the lake";
        public const string EarlyName = "Early Marko";
        public const string BoundaryName = "Boundary Dime";
        public const string LateName = "Latecomer Petra";
        public const string BeforeTitle = "Dinner before leaving";
        public const string AfterTitle = "Hotel after leaving";
        public const decimal MkdPerEur = 61.5610m;

        public static readonly DateTimeOffset OwnerJoinedAt = ExpensesApp.ClockStart;
        public static readonly DateTimeOffset CallerJoinedAt = OwnerJoinedAt + ExpensesApp.JoiningGap;
        public static readonly DateTimeOffset EarlyJoinedAt = OwnerJoinedAt + (ExpensesApp.JoiningGap * 2);
        public static readonly DateTimeOffset BeforeExpenseAt = OwnerJoinedAt + (ExpensesApp.JoiningGap * 5);
        public static readonly DateTimeOffset BeforePaymentAt = OwnerJoinedAt + (ExpensesApp.JoiningGap * 6);
        public static readonly DateTimeOffset EndedAt = OwnerJoinedAt + (ExpensesApp.JoiningGap * 10);
        public static readonly DateTimeOffset LateJoinedAt = OwnerJoinedAt + (ExpensesApp.JoiningGap * 11);
        public static readonly DateTimeOffset AfterExpenseAt = OwnerJoinedAt + (ExpensesApp.JoiningGap * 12);
        public static readonly DateTimeOffset AfterPaymentAt = OwnerJoinedAt + (ExpensesApp.JoiningGap * 13);
        public static readonly DateTimeOffset EarlyEndedAt = OwnerJoinedAt + (ExpensesApp.JoiningGap * 15);
        public static readonly DateTimeOffset RejoinedAt = OwnerJoinedAt + (ExpensesApp.JoiningGap * 20);

        public static async Task<ExportLeftOutcome> CallerLeftAsync(ExpensesApp app)
        {
            ExportBeforeEnd before = await StartAsync(app);
            app.Clock.SetTo(EndedAt);
            HttpResponseMessage left = await GroupRequests.LeaveAsync(before.Caller.Client, before.GroupId);
            Assert.Equal(HttpStatusCode.NoContent, left.StatusCode);

            return await FinishAsync(app, before);
        }

        public static async Task<ExportLeftOutcome> CallerRemovedAsync(ExpensesApp app)
        {
            ExportBeforeEnd before = await StartAsync(app);
            app.Clock.SetTo(EndedAt);
            HttpResponseMessage removed = await GroupRequests.RemoveMemberAsync(before.Owner.Client, before.GroupId, before.CallerRowId);
            Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);

            return await FinishAsync(app, before);
        }

        public static async Task<ExportLeftOutcome> CallerLeftAndCameBackAsync(ExpensesApp app)
        {
            ExportBeforeEnd before = await StartAsync(app);
            app.Clock.SetTo(EndedAt);
            HttpResponseMessage left = await GroupRequests.LeaveAsync(before.Caller.Client, before.GroupId);
            Assert.Equal(HttpStatusCode.NoContent, left.StatusCode);
            ExportAfterEnd after = await AddWhatHappensAfterAsync(app, before);
            await GroupRows.InsertMemberAsync(app, before.GroupId, before.Caller.UserId, before.Caller.Form.DisplayName, before.Owner.UserId, joinedAt: RejoinedAt);

            return await ReadAsync(before, after);
        }

        public static async Task<ExportLeftOutcome> CallerNeverLeftAsync(ExpensesApp app)
        {
            ExportBeforeEnd before = await StartAsync(app);

            return await FinishAsync(app, before);
        }

        private static async Task<ExportBeforeEnd> StartAsync(ExpensesApp app)
        {
            app.Clock.SetTo(OwnerJoinedAt);
            SignedInUser owner = await app.SignUpAsync(GroupsApp.OwnerName);
            SignedInUser caller = await app.SignUpAsync(GroupsApp.MemberName);
            Guid groupId = await GroupRows.InsertGroupWithOwnerAsync(app, owner.UserId, name: GroupName, ownerJoinedAt: OwnerJoinedAt);
            Guid ownerRowId = await GroupRows.CurrentMemberIdAsync(app, groupId, owner.UserId);
            Guid callerRowId = await GroupRows.InsertMemberAsync(app, groupId, caller.UserId, caller.Form.DisplayName, owner.UserId, joinedAt: CallerJoinedAt);
            Guid earlyRowId = await GroupRows.InsertMemberAsync(app, groupId, null, EarlyName, owner.UserId, joinedAt: EarlyJoinedAt);
            await BudgetCurrencySteps.UseRateAsync(app, MkdPerEur);

            app.Clock.SetTo(BeforeExpenseAt);
            ExpenseInput dinner = ExpenseInputs.Equal(100_000, ExpenseInputs.Mkd, callerRowId, callerRowId, ownerRowId) with { Title = BeforeTitle };
            Guid beforeExpenseId = await app.AddExpenseAsync(caller, groupId, dinner);

            app.Clock.SetTo(BeforePaymentAt);
            Guid beforePaymentId = await SettlementSteps.RecordAsync(owner, groupId, ownerRowId, callerRowId, 50_000, ExpenseInputs.Mkd);
            await SettlementSteps.ConfirmAsync(caller, groupId, beforePaymentId);

            return new ExportBeforeEnd(owner, caller, groupId, ownerRowId, callerRowId, earlyRowId, beforeExpenseId, beforePaymentId);
        }

        private static async Task<ExportAfterEnd> AddWhatHappensAfterAsync(ExpensesApp app, ExportBeforeEnd before)
        {
            Guid boundaryRowId = await GroupRows.InsertMemberAsync(app, before.GroupId, null, BoundaryName, before.Owner.UserId, joinedAt: EndedAt);
            Guid lateRowId = await GroupRows.InsertMemberAsync(app, before.GroupId, null, LateName, before.Owner.UserId, joinedAt: LateJoinedAt);

            app.Clock.SetTo(AfterExpenseAt);
            ExpenseInput hotel = ExpenseInputs.Equal(90_000, ExpenseInputs.Mkd, before.OwnerRowId, before.OwnerRowId, before.EarlyRowId, lateRowId) with { Title = AfterTitle };
            Guid afterExpenseId = await app.AddExpenseAsync(before.Owner, before.GroupId, hotel);

            app.Clock.SetTo(AfterPaymentAt);
            Guid afterPaymentId = await SettlementSteps.RecordAsync(before.Owner, before.GroupId, before.EarlyRowId, before.OwnerRowId, 30_000, ExpenseInputs.Mkd);

            await ExportRows.EndMemberRowAtAsync(app, before.EarlyRowId, before.Owner.UserId, EarlyEndedAt, "Removed");

            return new ExportAfterEnd(boundaryRowId, lateRowId, afterExpenseId, afterPaymentId);
        }

        private static async Task<ExportLeftOutcome> FinishAsync(ExpensesApp app, ExportBeforeEnd before)
        {
            ExportAfterEnd after = await AddWhatHappensAfterAsync(app, before);

            return await ReadAsync(before, after);
        }

        private static async Task<ExportLeftOutcome> ReadAsync(ExportBeforeEnd before, ExportAfterEnd after)
        {
            ExportBody body = await ExportRequests.GetBodyAsync(before.Caller.Client);

            return new ExportLeftOutcome(before, after, body);
        }
    }
}
