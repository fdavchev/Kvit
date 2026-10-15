using Kvit.Api.Tests.Groups;
using Kvit.Domain.Results;

namespace Kvit.Api.Tests.Guards
{
    public static class MemberWrites
    {
        public static IReadOnlyList<GuardedWrite> All { get; } =
        [
            new GuardedWrite
            {
                Name = "AddPlainName",
                Who = WhoMayCall.OwnerAndMembers,
                PrepareAsync = (_, group) => Task.FromResult<SendWrite>(
                    caller => GroupRequests.AddMemberAsync(caller, group.GroupId, GuardSteps.NewPlainName)),
            },
            new GuardedWrite
            {
                Name = "ClaimName",
                Who = WhoMayCall.MemberOnly,
                PrepareAsync = (_, group) => Task.FromResult<SendWrite>(
                    caller => GroupRequests.ClaimAsync(caller, group.GroupId, group.MarkoRowId)),
            },
            new GuardedWrite
            {
                Name = "Leave",
                Who = WhoMayCall.MemberOnly,
                IsAllowedWhenFinished = true,
                PrepareAsync = (_, group) => Task.FromResult<SendWrite>(
                    caller => GroupRequests.LeaveAsync(caller, group.GroupId)),
            },
            new GuardedWrite
            {
                Name = "UndoClaim",
                Who = WhoMayCall.OwnerOnly,
                EffectOnClosing = EffectOnClosing.CancelsIt,
                CodeWhenAnotherMemberTries = ResultCodes.GROUP_NOT_OWNER,
                PrepareAsync = async (_, group) =>
                {
                    await GuardSteps.AssertNoContentAsync(GroupRequests.ClaimAsync(group.Third.Client, group.GroupId, group.CvetaRowId));

                    return caller => GroupRequests.UndoClaimAsync(caller, group.GroupId, group.CvetaRowId);
                },
            },
            new GuardedWrite
            {
                Name = "RemoveMember",
                Who = WhoMayCall.OwnerOnly,
                EffectOnClosing = EffectOnClosing.CancelsIt,
                CodeWhenAnotherMemberTries = ResultCodes.GROUP_NOT_OWNER,
                PrepareAsync = (_, group) => Task.FromResult<SendWrite>(
                    caller => GroupRequests.RemoveMemberAsync(caller, group.GroupId, group.ThirdRowId)),
            },
            new GuardedWrite
            {
                Name = "LetBackIn",
                Who = WhoMayCall.OwnerOnly,
                EffectOnClosing = EffectOnClosing.CancelsIt,
                HasNoMemberToConfirmBeforeTheWrite = true,
                CodeWhenAnotherMemberTries = ResultCodes.GROUP_NOT_OWNER,
                PrepareAsync = async (_, group) =>
                {
                    await GuardSteps.AssertNoContentAsync(GroupRequests.RemoveMemberAsync(group.Owner.Client, group.GroupId, group.MemberRowId));

                    return caller => GroupRequests.LetBackInAsync(caller, group.GroupId, group.MemberRowId);
                },
            },
            new GuardedWrite
            {
                Name = "MakeOwner",
                Who = WhoMayCall.OwnerOnly,
                EffectOnClosing = EffectOnClosing.CancelsIt,
                CodeWhenAnotherMemberTries = ResultCodes.GROUP_NOT_OWNER,
                PrepareAsync = (_, group) => Task.FromResult<SendWrite>(
                    caller => GroupRequests.MakeOwnerAsync(caller, group.GroupId, group.MemberRowId)),
            },
        ];
    }
}
