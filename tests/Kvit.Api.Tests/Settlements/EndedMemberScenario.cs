using System.Net;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Xunit;

namespace Kvit.Api.Tests.Settlements
{
    public enum PaymentSide
    {
        Payer,
        Receiver,
    }

    public static class EndedMemberScenario
    {
        public const string Left = "Left";
        public const string Removed = "Removed";

        public static async Task<Guid> RecordPaymentThatSettlesTheDebtAsync(ExpensesApp app, SettlementGroup group, PaymentSide bojanIs)
        {
            if (bojanIs == PaymentSide.Payer)
            {
                await ZeroBalanceScenario.BojanOwesAnaAsync(app, group);
                return await RecordConfirmedPaymentAsync(group, PaymentSide.Payer);
            }

            await ZeroBalanceScenario.AnaOwesBojanAsync(app, group);
            return await RecordConfirmedPaymentAsync(group, PaymentSide.Receiver);
        }

        public static Task<Guid> RecordConfirmedPaymentAsync(SettlementGroup group, PaymentSide bojanIs)
        {
            if (bojanIs == PaymentSide.Payer)
            {
                return SettlementSteps.RecordAsync(group.Owner, group.GroupId, group.MemberRowId, group.OwnerRowId);
            }

            return SettlementSteps.RecordAsync(group.Member, group.GroupId, group.OwnerRowId, group.MemberRowId);
        }

        public static async Task EndBojanMembershipAsync(SettlementGroup group, string endKind)
        {
            HttpResponseMessage response = endKind switch
            {
                Left => await GroupRequests.LeaveAsync(group.Member.Client, group.GroupId),
                Removed => await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MemberRowId),
                _ => throw new ArgumentException($"{endKind} is not a way to end a membership; use {Left} or {Removed}.", nameof(endKind)),
            };

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public static async Task RemoveMarkoAsync(SettlementGroup group)
        {
            HttpResponseMessage response = await GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MarkoRowId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
