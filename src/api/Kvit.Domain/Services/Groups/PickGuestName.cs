using Kvit.Domain.Accounts;
using Kvit.Domain.Entities;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;

namespace Kvit.Domain.Services.Groups
{
    public sealed class PickGuestName(
        IGroupRepository _groups,
        IGroupMemberRepository _members)
    {
        public async Task<Result<string>> Execute(string inviteToken, GuestJoinChoice choice, CancellationToken cancellationToken)
        {
            Result<Group> found = await _groups.FindByInviteTokenAsync(inviteToken, cancellationToken);
            if (!found.IsSuccess)
            {
                return found.ToFailure<string>();
            }

            GroupRoster roster = await _members.RosterOfAsync(found.Value.Id, cancellationToken);
            if (choice.ClaimMemberId is Guid claimMemberId)
            {
                Result<NamedMember> freeName = roster.FindCurrentPlainName(claimMemberId);
                if (!freeName.IsSuccess)
                {
                    return freeName.ToFailure<string>();
                }

                return Result.Ok(freeName.Value.Member.Name);
            }

            string newName = choice.NewName
                ?? throw new InvalidOperationException("A guest join choice has neither a free name to take nor a new name.");
            Result nameFree = roster.CheckNameFree(newName);
            if (!nameFree.IsSuccess)
            {
                return nameFree.ToFailure<string>();
            }

            return Result.Ok(newName);
        }
    }
}
