using System.Text.Json;
using Xunit;

namespace Kvit.Api.Tests.Export
{
    public class ExportLeftGroupTests(ExportLeftApp _app) : IClassFixture<ExportLeftApp>
    {
        [Fact]
        public async Task Left_ExpenseCreatedBeforeLeaving_IsListedWithItsShares()
        {
            ExportLeftOutcome outcome = await _app.CallerLeftAsync();

            JsonElement expense = ExportBody.WithId(outcome.Expenses, outcome.Before.BeforeExpenseId);

            Assert.Equal(ExportLeftSteps.BeforeTitle, expense.GetProperty("title").GetString());
            Assert.Equal(
                new Dictionary<Guid, long> { [outcome.Before.CallerRowId] = 50_000, [outcome.Before.OwnerRowId] = 50_000 },
                SharesOf(expense));
        }

        [Fact]
        public async Task Left_ExpenseCreatedAfterLeaving_IsNotListed()
        {
            ExportLeftOutcome outcome = await _app.CallerLeftAsync();

            Assert.Equal([outcome.Before.BeforeExpenseId], ExportBody.IdsOf(outcome.Expenses));
        }

        [Fact]
        public async Task Left_PaymentCreatedBeforeLeaving_IsListed()
        {
            ExportLeftOutcome outcome = await _app.CallerLeftAsync();

            JsonElement payment = ExportBody.WithId(outcome.Payments, outcome.Before.BeforePaymentId);

            Assert.Equal("Confirmed", payment.GetProperty("status").GetString());
            Assert.Equal(50_000, payment.GetProperty("amountMinor").GetInt64());
        }

        [Fact]
        public async Task Left_PaymentCreatedAfterLeaving_IsNotListed()
        {
            ExportLeftOutcome outcome = await _app.CallerLeftAsync();

            Assert.Equal([outcome.Before.BeforePaymentId], ExportBody.IdsOf(outcome.Payments));
        }

        [Fact]
        public async Task Left_MemberWhoJoinedAfterLeaving_IsNotListed()
        {
            ExportLeftOutcome outcome = await _app.CallerLeftAsync();

            Assert.DoesNotContain(outcome.After.LateRowId, MemberIdsOf(outcome));
        }

        [Fact]
        public async Task Left_MemberWhoJoinedAtTheMomentOfLeaving_IsListed()
        {
            ExportLeftOutcome outcome = await _app.CallerLeftAsync();

            Assert.Contains(outcome.After.BoundaryRowId, MemberIdsOf(outcome));
        }

        [Fact]
        public async Task Left_MembersWhoJoinedBeforeLeaving_AreListed()
        {
            ExportLeftOutcome outcome = await _app.CallerLeftAsync();

            Assert.Equal(
                new[] { outcome.Before.OwnerRowId, outcome.Before.CallerRowId, outcome.Before.EarlyRowId, outcome.After.BoundaryRowId }.Order(),
                MemberIdsOf(outcome).Order());
        }

        [Fact]
        public async Task Left_OwnMembership_ShowsEndKindLeftAndTheMomentOfLeaving()
        {
            ExportLeftOutcome outcome = await _app.CallerLeftAsync();

            Assert.Equal(outcome.Before.CallerRowId, outcome.Membership.GetProperty("memberId").GetGuid());
            Assert.Equal("Left", outcome.Membership.GetProperty("endKind").GetString());
            Assert.Equal(ExportLeftSteps.EndedAt, outcome.Membership.GetProperty("removedAt").GetDateTimeOffset());
        }

        [Fact]
        public async Task Left_MemberWhoWasRemovedAfterTheCallerLeft_IsShownAsStillActive()
        {
            ExportLeftOutcome outcome = await _app.CallerLeftAsync();

            JsonElement early = ExportBody.WithId(outcome.Members, outcome.Before.EarlyRowId, "memberId");

            Assert.Null(ExportBody.StringOrNull(early, "endKind"));
            Assert.Null(ExportBody.StringOrNull(early, "removedAt"));
        }

        [Fact]
        public async Task Left_CallersOwnRowInTheMemberList_ShowsEndKindLeftAndRemovedAt()
        {
            ExportLeftOutcome outcome = await _app.CallerLeftAsync();

            JsonElement you = ExportBody.WithId(outcome.Members, outcome.Before.CallerRowId, "memberId");

            Assert.True(you.GetProperty("isYou").GetBoolean());
            Assert.Equal("Left", you.GetProperty("endKind").GetString());
            Assert.Equal(ExportLeftSteps.EndedAt, you.GetProperty("removedAt").GetDateTimeOffset());
        }

        [Fact]
        public async Task Left_SharesOfListedExpenses_OnlyPointAtListedMembers()
        {
            ExportLeftOutcome outcome = await _app.CallerLeftAsync();

            List<Guid> listedMembers = MemberIdsOf(outcome);

            Assert.All(outcome.Expenses, expense => Assert.All(SharesOf(expense).Keys, memberId => Assert.Contains(memberId, listedMembers)));
        }

        [Fact]
        public async Task Left_RawBody_NeverMentionsTheLaterExpenseTheLaterPaymentOrTheLaterMember()
        {
            ExportLeftOutcome outcome = await _app.CallerLeftAsync();

            string[] laterValues =
            [
                ExportLeftSteps.AfterTitle,
                ExportLeftSteps.LateName,
                outcome.After.AfterExpenseId.ToString(),
                outcome.After.AfterPaymentId.ToString(),
                outcome.After.LateRowId.ToString(),
            ];
            foreach (string value in laterValues)
            {
                outcome.Body.AssertDoesNotMention(value);
            }
        }

        [Fact]
        public async Task Removed_ExpenseCreatedBeforeBeingRemoved_IsListed()
        {
            ExportLeftOutcome outcome = await _app.CallerRemovedAsync();

            JsonElement expense = ExportBody.WithId(outcome.Expenses, outcome.Before.BeforeExpenseId);

            Assert.Equal(ExportLeftSteps.BeforeTitle, expense.GetProperty("title").GetString());
        }

        [Fact]
        public async Task Removed_ExpenseCreatedAfterBeingRemoved_IsNotListed()
        {
            ExportLeftOutcome outcome = await _app.CallerRemovedAsync();

            Assert.Equal([outcome.Before.BeforeExpenseId], ExportBody.IdsOf(outcome.Expenses));
        }

        [Fact]
        public async Task Removed_PaymentCreatedBeforeBeingRemoved_IsListed()
        {
            ExportLeftOutcome outcome = await _app.CallerRemovedAsync();

            Assert.Contains(outcome.Before.BeforePaymentId, ExportBody.IdsOf(outcome.Payments));
        }

        [Fact]
        public async Task Removed_PaymentCreatedAfterBeingRemoved_IsNotListed()
        {
            ExportLeftOutcome outcome = await _app.CallerRemovedAsync();

            Assert.DoesNotContain(outcome.After.AfterPaymentId, ExportBody.IdsOf(outcome.Payments));
        }

        [Fact]
        public async Task Removed_MemberWhoJoinedAfterBeingRemoved_IsNotListed()
        {
            ExportLeftOutcome outcome = await _app.CallerRemovedAsync();

            Assert.DoesNotContain(outcome.After.LateRowId, MemberIdsOf(outcome));
        }

        [Fact]
        public async Task Removed_OwnMembership_ShowsEndKindRemovedAndTheMomentOfRemoval()
        {
            ExportLeftOutcome outcome = await _app.CallerRemovedAsync();

            Assert.Equal("Removed", outcome.Membership.GetProperty("endKind").GetString());
            Assert.Equal(ExportLeftSteps.EndedAt, outcome.Membership.GetProperty("removedAt").GetDateTimeOffset());
        }

        [Fact]
        public async Task Removed_MemberWhoWasRemovedAfterTheCaller_IsShownAsStillActive()
        {
            ExportLeftOutcome outcome = await _app.CallerRemovedAsync();

            JsonElement early = ExportBody.WithId(outcome.Members, outcome.Before.EarlyRowId, "memberId");

            Assert.Null(ExportBody.StringOrNull(early, "endKind"));
            Assert.Null(ExportBody.StringOrNull(early, "removedAt"));
        }

        [Fact]
        public async Task Removed_RawBody_NeverMentionsTheLaterExpenseTheLaterPaymentOrTheLaterMember()
        {
            ExportLeftOutcome outcome = await _app.CallerRemovedAsync();

            string[] laterValues =
            [
                ExportLeftSteps.AfterTitle,
                ExportLeftSteps.LateName,
                outcome.After.AfterExpenseId.ToString(),
                outcome.After.AfterPaymentId.ToString(),
                outcome.After.LateRowId.ToString(),
            ];
            foreach (string value in laterValues)
            {
                outcome.Body.AssertDoesNotMention(value);
            }
        }

        [Fact]
        public async Task CameBack_MembershipIsTheCurrentOneWithNoEnd()
        {
            ExportLeftOutcome outcome = await _app.CallerCameBackAsync();

            Assert.NotEqual(outcome.Before.CallerRowId, outcome.Membership.GetProperty("memberId").GetGuid());
            Assert.Equal(ExportLeftSteps.RejoinedAt, outcome.Membership.GetProperty("joinedAt").GetDateTimeOffset());
            Assert.Null(ExportBody.StringOrNull(outcome.Membership, "endKind"));
            Assert.Null(ExportBody.StringOrNull(outcome.Membership, "removedAt"));
        }

        [Fact]
        public async Task CameBack_ExpensesCreatedWhileAway_AreListed()
        {
            ExportLeftOutcome outcome = await _app.CallerCameBackAsync();

            Assert.Equal(
                new[] { outcome.Before.BeforeExpenseId, outcome.After.AfterExpenseId }.Order(),
                ExportBody.IdsOf(outcome.Expenses).Order());
        }

        [Fact]
        public async Task CameBack_PaymentsCreatedWhileAway_AreListed()
        {
            ExportLeftOutcome outcome = await _app.CallerCameBackAsync();

            Assert.Equal(
                new[] { outcome.Before.BeforePaymentId, outcome.After.AfterPaymentId }.Order(),
                ExportBody.IdsOf(outcome.Payments).Order());
        }

        [Fact]
        public async Task CameBack_MemberWhoJoinedWhileAway_IsListed()
        {
            ExportLeftOutcome outcome = await _app.CallerCameBackAsync();

            Assert.Contains(outcome.After.LateRowId, MemberIdsOf(outcome));
        }

        [Fact]
        public async Task StillAMember_ExpensesCreatedAfterTheMomentLeavingWouldHaveHappened_AreListed()
        {
            ExportLeftOutcome outcome = await _app.CallerNeverLeftAsync();

            Assert.Equal(
                new[] { outcome.Before.BeforeExpenseId, outcome.After.AfterExpenseId }.Order(),
                ExportBody.IdsOf(outcome.Expenses).Order());
        }

        [Fact]
        public async Task StillAMember_PaymentsAreListed()
        {
            ExportLeftOutcome outcome = await _app.CallerNeverLeftAsync();

            Assert.Equal(
                new[] { outcome.Before.BeforePaymentId, outcome.After.AfterPaymentId }.Order(),
                ExportBody.IdsOf(outcome.Payments).Order());
        }

        [Fact]
        public async Task StillAMember_EveryMemberIsListedWithTheirOwnEnd()
        {
            ExportLeftOutcome outcome = await _app.CallerNeverLeftAsync();

            Assert.Equal(
                new[] { outcome.Before.OwnerRowId, outcome.Before.CallerRowId, outcome.Before.EarlyRowId, outcome.After.BoundaryRowId, outcome.After.LateRowId }.Order(),
                MemberIdsOf(outcome).Order());
            JsonElement early = ExportBody.WithId(outcome.Members, outcome.Before.EarlyRowId, "memberId");
            Assert.Equal("Removed", early.GetProperty("endKind").GetString());
            Assert.Equal(ExportLeftSteps.EarlyEndedAt, early.GetProperty("removedAt").GetDateTimeOffset());
        }

        [Fact]
        public async Task StillAMember_OwnMembership_HasNoEnd()
        {
            ExportLeftOutcome outcome = await _app.CallerNeverLeftAsync();

            Assert.Null(ExportBody.StringOrNull(outcome.Membership, "endKind"));
            Assert.Null(ExportBody.StringOrNull(outcome.Membership, "removedAt"));
        }

        private static List<Guid> MemberIdsOf(ExportLeftOutcome outcome)
        {
            return ExportBody.IdsOf(outcome.Members, "memberId");
        }

        private static Dictionary<Guid, long> SharesOf(JsonElement expense)
        {
            return ExportBody.ListOf(expense, "shares")
                .ToDictionary(share => share.GetProperty("memberId").GetGuid(), share => share.GetProperty("amountMinor").GetInt64());
        }
    }
}
