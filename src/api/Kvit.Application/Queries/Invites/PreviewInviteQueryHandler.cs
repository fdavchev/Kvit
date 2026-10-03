using Kvit.Application.Dispatching;
using Kvit.Contracts.Auth;
using Kvit.Contracts.Invites;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Application.Queries.Invites
{
    public sealed class PreviewInviteQueryHandler(
        ICurrentUserProvider _currentUserProvider,
        AppDbContext _context) : IQueryHandler<PreviewInviteQuery, InvitePreviewResponse>
    {
        public async Task<Result<InvitePreviewResponse>> Handle(PreviewInviteQuery query, CancellationToken cancellationToken)
        {
            InvitedGroupDto? group = await _context.Groups
                .AsNoTracking()
                .Where(candidate => candidate.InviteToken == query.Token && candidate.DeletedAt == null)
                .Select(candidate => new InvitedGroupDto(candidate.Id, candidate.Name, candidate.Emoji))
                .SingleOrDefaultAsync(cancellationToken);
            if (group is null)
            {
                return Group.InviteNotFound<InvitePreviewResponse>();
            }

            List<InvitedMemberDto> members = await InvitedMembersOf(group.Id).ToListAsync(cancellationToken);
            InvitePreviewStatus status = await StatusOfCallerAsync(group.Id, cancellationToken);

            return Result.Ok(new InvitePreviewResponse(
                status,
                status == InvitePreviewStatus.AlreadyMember ? group.Id : null,
                group.Name,
                group.Emoji,
                [.. members.Select(member => member.DisplayName)],
                [.. members.Where(member => member.IsNameOnly).Select(member => new UnclaimedNameRow(member.Id, member.DisplayName))]));
        }

        private IQueryable<InvitedMemberDto> InvitedMembersOf(Guid groupId)
        {
            return _context.NamedGroupMembers(groupId)
                .AsNoTracking()
                .Where(named => named.Member.RemovedAt == null)
                .InJoiningOrder()
                .Select(named => new InvitedMemberDto(named.Member.Id, named.DisplayName, named.Member.UserId == null));
        }

        private async Task<InvitePreviewStatus> StatusOfCallerAsync(Guid groupId, CancellationToken cancellationToken)
        {
            Result<Guid> userId = _currentUserProvider.GetCurrentUserId();
            if (!userId.IsSuccess)
            {
                return InvitePreviewStatus.Open;
            }

            IQueryable<GroupMember> callerRows = _context.GroupMembers.Where(member => member.GroupId == groupId && member.UserId == userId.Value);
            if (await callerRows.AnyAsync(member => member.RemovedAt == null, cancellationToken))
            {
                return InvitePreviewStatus.AlreadyMember;
            }

            if (await callerRows.AnyAsync(member => member.EndKind == MemberEndKind.Removed, cancellationToken))
            {
                return InvitePreviewStatus.Removed;
            }

            return InvitePreviewStatus.Open;
        }
    }
}
