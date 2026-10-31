using Kvit.Api.Tests.GroupPlan;
using Kvit.Api.Tests.Groups;
using Kvit.Domain.Results;

namespace Kvit.Api.Tests.Guards
{
    public static class GroupWrites
    {
        public static IReadOnlyList<GuardedWrite> All { get; } =
        [
            new GuardedWrite
            {
                Name = "UpdateGroup",
                Who = WhoMayCall.OwnerOnly,
                CodeWhenAnotherMemberTries = ResultCodes.GROUP_NOT_OWNER,
                PrepareAsync = (_, group) => Task.FromResult<SendWrite>(
                    caller => GroupRequests.UpdateAsync(caller, group.GroupId, GuardSteps.RenamedGroup)),
            },
            new GuardedWrite
            {
                Name = "SetGroupPlan",
                Who = WhoMayCall.OwnerOnly,
                CodeWhenAnotherMemberTries = ResultCodes.GROUP_NOT_OWNER,
                PrepareAsync = (_, group) => Task.FromResult<SendWrite>(
                    caller => GroupPlanRequests.SetAsync(caller, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd)),
            },
            new GuardedWrite
            {
                Name = "RemoveGroupPlan",
                Who = WhoMayCall.OwnerOnly,
                CodeWhenAnotherMemberTries = ResultCodes.GROUP_NOT_OWNER,
                PrepareAsync = async (_, group) =>
                {
                    await GroupPlanSteps.SetAsync(group.Owner, group.GroupId, GroupPlanSteps.TwoThousandMkd, GroupPlanRequests.Mkd);

                    return caller => GroupPlanRequests.DeleteAsync(caller, group.GroupId);
                },
            },
            new GuardedWrite
            {
                Name = "DeleteGroup",
                Who = WhoMayCall.OwnerOnly,
                IsAllowedWhenFinished = true,
                CodeWhenAnotherMemberTries = ResultCodes.GROUP_NOT_OWNER,
                PrepareAsync = (_, group) => Task.FromResult<SendWrite>(
                    caller => GroupRequests.DeleteAsync(caller, group.GroupId)),
            },
            new GuardedWrite
            {
                Name = "RestoreGroup",
                Who = WhoMayCall.OwnerOnly,
                IsAllowedWhenFinished = true,
                CodeWhenAnotherMemberTries = ResultCodes.GROUP_NOT_OWNER,
                PrepareAsync = (_, group) => Task.FromResult<SendWrite>(
                    async caller =>
                    {
                        await GuardSteps.AssertNoContentAsync(GroupRequests.DeleteAsync(group.Owner.Client, group.GroupId));

                        return await GroupRequests.RestoreAsync(caller, group.GroupId);
                    }),
            },
            new GuardedWrite
            {
                Name = "ResetInvite",
                Who = WhoMayCall.OwnerOnly,
                CodeWhenAnotherMemberTries = ResultCodes.GROUP_NOT_OWNER,
                PrepareAsync = (_, group) => Task.FromResult<SendWrite>(
                    caller => GroupRequests.ResetInviteAsync(caller, group.GroupId)),
            },
            new GuardedWrite
            {
                Name = "UndoInviteReset",
                Who = WhoMayCall.OwnerOnly,
                CodeWhenAnotherMemberTries = ResultCodes.GROUP_NOT_OWNER,
                PrepareAsync = async (_, group) =>
                {
                    await GuardSteps.AssertOkAsync(GroupRequests.ResetInviteAsync(group.Owner.Client, group.GroupId));

                    return caller => GroupRequests.UndoResetInviteAsync(caller, group.GroupId);
                },
            },
        ];
    }
}
