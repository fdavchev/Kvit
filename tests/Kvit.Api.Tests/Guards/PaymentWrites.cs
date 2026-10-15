using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Settlements;
using Kvit.Domain.Results;

namespace Kvit.Api.Tests.Guards
{
    public static class PaymentWrites
    {
        public static IReadOnlyList<GuardedWrite> All { get; } =
        [
            new GuardedWrite
            {
                Name = "RecordPayment",
                Who = WhoMayCall.OwnerAndMembers,
                EffectOnClosing = EffectOnClosing.CancelsIt,
                PrepareAsync = (_, group) => Task.FromResult<SendWrite>(
                    caller => SettlementRequests.RecordAsync(caller, group.GroupId, group.MemberRowId, group.OwnerRowId, ZeroBalanceScenario.ShareMinor, ExpenseInputs.Mkd)),
            },
            new GuardedWrite
            {
                Name = "ConfirmPayment",
                Who = WhoMayCall.MemberOnly,
                NeedsPendingPayment = true,
                CodeWhenAnotherMemberTries = ResultCodes.SETTLEMENT_NOT_ALLOWED,
                PrepareAsync = async (_, group) =>
                {
                    Guid settlementId = await SettlementSteps.PendingAnaToBojanAsync(group);

                    return caller => SettlementRequests.ConfirmAsync(caller, group.GroupId, settlementId);
                },
            },
            new GuardedWrite
            {
                Name = "RejectPayment",
                Who = WhoMayCall.MemberOnly,
                NeedsPendingPayment = true,
                CodeWhenAnotherMemberTries = ResultCodes.SETTLEMENT_NOT_ALLOWED,
                PrepareAsync = async (_, group) =>
                {
                    Guid settlementId = await SettlementSteps.PendingAnaToBojanAsync(group);

                    return caller => SettlementRequests.RejectAsync(caller, group.GroupId, settlementId);
                },
            },
            new GuardedWrite
            {
                Name = "CancelPayment",
                Who = WhoMayCall.MemberOnly,
                NeedsPendingPayment = true,
                CodeWhenAnotherMemberTries = ResultCodes.SETTLEMENT_NOT_ALLOWED,
                PrepareAsync = async (_, group) =>
                {
                    Guid settlementId = await SettlementSteps.PendingBojanToAnaAsync(group);

                    return caller => SettlementRequests.CancelAsync(caller, group.GroupId, settlementId);
                },
            },
            new GuardedWrite
            {
                Name = "ConfirmPaymentForAPlainName",
                Who = WhoMayCall.OwnerOnly,
                EffectOnClosing = EffectOnClosing.CancelsIt,
                NeedsPendingPayment = true,
                CodeWhenAnotherMemberTries = ResultCodes.SETTLEMENT_NOT_ALLOWED,
                PrepareAsync = async (_, group) =>
                {
                    Guid settlementId = await SettlementSteps.PendingBojanToMarkoAsync(group);

                    return caller => SettlementRequests.ConfirmAsync(caller, group.GroupId, settlementId);
                },
            },
            new GuardedWrite
            {
                Name = "RejectPaymentForAPlainName",
                Who = WhoMayCall.OwnerOnly,
                EffectOnClosing = EffectOnClosing.CancelsIt,
                NeedsPendingPayment = true,
                CodeWhenAnotherMemberTries = ResultCodes.SETTLEMENT_NOT_ALLOWED,
                PrepareAsync = async (_, group) =>
                {
                    Guid settlementId = await SettlementSteps.PendingBojanToMarkoAsync(group);

                    return caller => SettlementRequests.RejectAsync(caller, group.GroupId, settlementId);
                },
            },
            new GuardedWrite
            {
                Name = "CancelPaymentForAPlainName",
                Who = WhoMayCall.OwnerOnly,
                EffectOnClosing = EffectOnClosing.CancelsIt,
                NeedsPendingPayment = true,
                CodeWhenAnotherMemberTries = ResultCodes.SETTLEMENT_NOT_ALLOWED,
                PrepareAsync = async (_, group) =>
                {
                    Guid settlementId = await SettlementSteps.PendingMarkoToBojanByTheOwnerAsync(group);

                    return caller => SettlementRequests.CancelAsync(caller, group.GroupId, settlementId);
                },
            },
            new GuardedWrite
            {
                Name = "DeletePayment",
                Who = WhoMayCall.OwnerAndMembers,
                EffectOnClosing = EffectOnClosing.CancelsIt,
                CodeWhenAnotherMemberTries = ResultCodes.SETTLEMENT_NOT_ALLOWED,
                PrepareAsync = async (app, group) =>
                {
                    await ZeroBalanceScenario.AnaOwesBojanAsync(app, group);
                    Guid settlementId = await GuardSteps.RecordConfirmedPaymentByMemberAsync(group);

                    return caller => SettlementRequests.DeleteAsync(caller, group.GroupId, settlementId);
                },
            },
            new GuardedWrite
            {
                Name = "RestorePayment",
                Who = WhoMayCall.OwnerAndMembers,
                EffectOnClosing = EffectOnClosing.CancelsIt,
                CodeWhenAnotherMemberTries = ResultCodes.SETTLEMENT_NOT_ALLOWED,
                PrepareAsync = async (_, group) =>
                {
                    Guid settlementId = await GuardSteps.RecordConfirmedPaymentByMemberAsync(group);
                    await SettlementSteps.DeleteAsync(group.Member, group.GroupId, settlementId);

                    return caller => SettlementRequests.RestoreAsync(caller, group.GroupId, settlementId);
                },
            },
        ];
    }
}
