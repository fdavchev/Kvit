using Kvit.Contracts.Auth;
using Kvit.Domain.Accounts;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kvit.Infrastructure.Auth
{
    public sealed class AccountPurger(AppDbContext _context, ILookupNormalizer _normalizer) : IAccountPurger
    {
        private static readonly string[] MemberNameEventTypes =
        [
            nameof(ActivityEventType.MemberAdded),
            nameof(ActivityEventType.MemberJoined),
            nameof(ActivityEventType.MemberRemoved),
            nameof(ActivityEventType.MemberLeft),
            nameof(ActivityEventType.MemberLetBackIn),
            nameof(ActivityEventType.GuestLetInAgain),
            nameof(ActivityEventType.OwnershipTransferred),
            nameof(ActivityEventType.MemberClaimed),
            nameof(ActivityEventType.ClaimUndone),
        ];

        private static readonly string[] ClaimEventTypes =
        [
            nameof(ActivityEventType.MemberClaimed),
            nameof(ActivityEventType.ClaimUndone),
        ];

        private static readonly string[] SettlementEventTypes =
        [
            nameof(ActivityEventType.SettlementRecorded),
            nameof(ActivityEventType.SettlementConfirmed),
            nameof(ActivityEventType.SettlementRejected),
            nameof(ActivityEventType.SettlementCancelled),
            nameof(ActivityEventType.SettlementDeleted),
            nameof(ActivityEventType.SettlementRestored),
        ];

        public async Task PurgeAccountAsync(Guid userId, DeletionNameChoice nameChoice, DateTimeOffset now, CancellationToken cancellationToken)
        {
            await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            AppUser user = await LockUserAsync(userId, cancellationToken)
                ?? throw new InvalidOperationException($"User {userId} was asked to be purged, but no such user exists.");
            if (user.PurgedAt is not null)
            {
                return;
            }

            await PurgeLockedUserAsync(user, nameChoice, now, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        public async Task<int> PurgeDueAccountsAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            DateTimeOffset latestDueRequest = AccountDeletionRules.LatestDueRequestTime(now);
            List<Guid> dueUserIds = await _context.Users
                .Where(user => user.PurgedAt == null && user.DeletionRequestedAt != null && user.DeletionRequestedAt <= latestDueRequest)
                .OrderBy(user => user.Id)
                .Select(user => user.Id)
                .ToListAsync(cancellationToken);

            int purgedCount = 0;
            foreach (Guid userId in dueUserIds)
            {
                _context.ChangeTracker.Clear();
                await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                AppUser? user = await LockUserAsync(userId, cancellationToken);
                if (user is not { PurgedAt: null, DeletionRequestedAt: DateTimeOffset requestedAt, DeletionNameChoice: DeletionNameChoice nameChoice }
                    || requestedAt > latestDueRequest)
                {
                    continue;
                }

                await PurgeLockedUserAsync(user, nameChoice, now, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                purgedCount++;
            }

            return purgedCount;
        }

        private async Task<AppUser?> LockUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            List<AppUser> users = await _context.Users
                .FromSql($"SELECT * FROM users WHERE id = {userId} FOR UPDATE")
                .ToListAsync(cancellationToken);

            return users.SingleOrDefault();
        }

        private async Task PurgeLockedUserAsync(AppUser user, DeletionNameChoice nameChoice, DateTimeOffset now, CancellationToken cancellationToken)
        {
            string oldName = user.DisplayName;
            string remainingName = AccountDeletionRules.RemainingNameOf(nameChoice, oldName, user.Language);

            await HandOverOwnedGroupsAsync(user.Id, now, cancellationToken);
            Dictionary<Guid, string> plainNameByGroup = await TurnMembershipsIntoPlainNamesAsync(user.Id, remainingName, cancellationToken);
            if (nameChoice == DeletionNameChoice.FormerMember)
            {
                foreach ((Guid groupId, string plainName) in plainNameByGroup)
                {
                    await ReplaceNameInActivityAsync(groupId, oldName, plainName, cancellationToken);
                }
            }

            await DeletePersonalDataAsync(user.Id, cancellationToken);
            await _context.UsageEvents
                .Where(usageEvent => usageEvent.UserId == user.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(usageEvent => usageEvent.UserId, (Guid?)null), cancellationToken);
            await DeleteSignInDataAsync(user.Id, cancellationToken);

            string userName = AppUser.TombstoneUserNameOf(user.Id);
            user.BecomeTombstone(nameChoice, remainingName, _normalizer.NormalizeName(userName), now);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task HandOverOwnedGroupsAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            List<Group> ownedGroups = await _context.Groups
                .FromSql($"SELECT * FROM groups WHERE owner_user_id = {userId} ORDER BY id FOR UPDATE")
                .ToListAsync(cancellationToken);

            foreach (Group group in ownedGroups)
            {
                NamedMember? heir = await HeirOf(group.Id, userId).FirstOrDefaultAsync(cancellationToken);
                if (heir is not null)
                {
                    Result transferred = group.TransferOwnership(heir);
                    if (!transferred.IsSuccess)
                    {
                        throw new InvalidOperationException($"Group {group.Id} could not be handed over to member {heir.Member.Id} while purging user {userId}: {transferred.Error}");
                    }

                    _context.ActivityEvents.Add(ActivityEvent.OwnershipTransferred(group.Id, userId, heir.Member.Id, heir.DisplayName, now));
                }
                else if (group.Status != GroupStatus.Finished)
                {
                    group.Finish(now);
                    _context.ActivityEvents.Add(ActivityEvent.GroupFinished(group.Id, userId, now));
                }
            }
        }

        private IQueryable<NamedMember> HeirOf(Guid groupId, Guid leavingOwnerUserId)
        {
            return _context.GroupMembers
                .AsNoTracking()
                .Where(member => member.GroupId == groupId && member.RemovedAt == null)
                .Join(_context.Users, member => member.UserId, user => (Guid?)user.Id, (member, user) => new { Member = member, User = user })
                .Where(row => !row.User.IsGuest && row.User.DeletionRequestedAt == null && row.User.Id != leavingOwnerUserId)
                .OrderBy(row => row.Member.JoinedAt)
                .ThenBy(row => row.Member.Id)
                .Select(row => new NamedMember { Member = row.Member, DisplayName = row.User.DisplayName, IsGuest = row.User.IsGuest });
        }

        private async Task<Dictionary<Guid, string>> TurnMembershipsIntoPlainNamesAsync(Guid userId, string remainingName, CancellationToken cancellationToken)
        {
            List<GroupMember> rows = await _context.GroupMembers
                .Where(member => member.UserId == userId)
                .OrderBy(member => member.GroupId)
                .ThenBy(member => member.RemovedAt != null)
                .ThenBy(member => member.JoinedAt)
                .ThenBy(member => member.Id)
                .ToListAsync(cancellationToken);

            Dictionary<Guid, string> plainNameByGroup = [];
            foreach (IGrouping<Guid, GroupMember> rowsInGroup in rows.GroupBy(row => row.GroupId))
            {
                List<string> takenNames = await _context.NamedGroupMembers(rowsInGroup.Key)
                    .Where(named => named.Member.UserId != userId)
                    .Select(named => named.DisplayName)
                    .ToListAsync(cancellationToken);

                foreach (GroupMember row in rowsInGroup)
                {
                    string plainName = AccountDeletionRules.FirstFreeName(remainingName, takenNames);
                    takenNames.Add(plainName);
                    row.TurnIntoPlainNameCalled(plainName);
                    plainNameByGroup.TryAdd(row.GroupId, plainName);
                }
            }

            return plainNameByGroup;
        }

        private async Task ReplaceNameInActivityAsync(Guid groupId, string oldName, string newName, CancellationToken cancellationToken)
        {
            await ReplaceNameInDataFieldAsync(groupId, "name", MemberNameEventTypes, oldName, newName, cancellationToken);
            await ReplaceNameInDataFieldAsync(groupId, "claimedName", ClaimEventTypes, oldName, newName, cancellationToken);
            await ReplaceNameInDataFieldAsync(groupId, "fromName", SettlementEventTypes, oldName, newName, cancellationToken);
            await ReplaceNameInDataFieldAsync(groupId, "toName", SettlementEventTypes, oldName, newName, cancellationToken);
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE activity_events
                SET changes = (
                    SELECT jsonb_agg(
                        CASE
                            WHEN item.change ->> 'field' = 'paidBy' THEN item.change
                                || CASE WHEN item.change ->> 'old' = {oldName} THEN jsonb_build_object('old', {newName}::text) ELSE jsonb_build_object() END
                                || CASE WHEN item.change ->> 'new' = {oldName} THEN jsonb_build_object('new', {newName}::text) ELSE jsonb_build_object() END
                            WHEN item.change ->> 'field' = 'split' THEN item.change
                                || CASE WHEN jsonb_typeof(item.change -> 'old') = 'string' THEN jsonb_build_object('old', replace(item.change ->> 'old', {oldName}, {newName})) ELSE jsonb_build_object() END
                                || CASE WHEN jsonb_typeof(item.change -> 'new') = 'string' THEN jsonb_build_object('new', replace(item.change ->> 'new', {oldName}, {newName})) ELSE jsonb_build_object() END
                            ELSE item.change
                        END
                        ORDER BY item.position)
                    FROM jsonb_array_elements(changes) WITH ORDINALITY AS item(change, position))
                WHERE group_id = {groupId}
                    AND jsonb_typeof(changes) = 'array'
                    AND EXISTS (
                        SELECT 1
                        FROM jsonb_array_elements(changes) AS candidate(change)
                        WHERE candidate.change ->> 'field' IN ('paidBy', 'split')
                            AND (strpos(candidate.change ->> 'old', {oldName}) > 0 OR strpos(candidate.change ->> 'new', {oldName}) > 0))
                """,
                cancellationToken);
        }

        private async Task ReplaceNameInDataFieldAsync(Guid groupId, string field, string[] eventTypes, string oldName, string newName, CancellationToken cancellationToken)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE activity_events
                SET data = data || jsonb_build_object({field}::text, {newName}::text)
                WHERE group_id = {groupId}
                    AND type = ANY({eventTypes})
                    AND data ->> {field} = {oldName}
                """,
                cancellationToken);
        }

        private async Task DeletePersonalDataAsync(Guid userId, CancellationToken cancellationToken)
        {
            await _context.DuplicateAnswers.Where(answer => answer.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await _context.PersonalEntries.Where(entry => entry.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await _context.RecurringIncomes.Where(income => income.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await _context.BudgetLimits.Where(limit => limit.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await _context.Categories.Where(category => category.OwnerUserId == userId).ExecuteDeleteAsync(cancellationToken);
        }

        private async Task DeleteSignInDataAsync(Guid userId, CancellationToken cancellationToken)
        {
            await _context.UserLogins.Where(login => login.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await _context.UserClaims.Where(claim => claim.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await _context.UserTokens.Where(token => token.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await _context.UserRoles.Where(role => role.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        }
    }
}
