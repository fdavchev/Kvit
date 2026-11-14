using Kvit.Contracts.Auth;
using Kvit.Contracts.OwnerTools;
using Kvit.Contracts.Persistence;
using Kvit.Domain.Accounts;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kvit.Infrastructure.Auth
{
    public sealed class AccountModeration(
        AppDbContext _context,
        IUnitOfWork _unitOfWork,
        IAccountPurger _accountPurger,
        TimeProvider _timeProvider) : IAccountModeration
    {
        public Task<Result> SuspendAsync(Guid adminUserId, Guid personId, string reason, CancellationToken cancellationToken)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();

            return ChangeLockedPersonAsync(
                adminUserId, personId, reason, AdminActionType.Suspended, TargetLabelOf, person => Task.FromResult(person.Suspend(now)), now, cancellationToken);
        }

        public Task<Result> UnsuspendAsync(Guid adminUserId, Guid personId, string reason, CancellationToken cancellationToken)
        {
            return ChangeLockedPersonAsync(
                adminUserId, personId, reason, AdminActionType.Unsuspended, TargetLabelOf, person => Task.FromResult(person.Unsuspend()), _timeProvider.GetUtcNow(), cancellationToken);
        }

        public Task<Result> EraseNameAsync(Guid adminUserId, Guid personId, string reason, CancellationToken cancellationToken)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();

            return ChangeLockedPersonAsync(
                adminUserId,
                personId,
                reason,
                AdminActionType.NameErased,
                _ => AdminAction.PurgedPersonLabel,
                person => _accountPurger.PurgeAccountAsync(person.Id, DeletionNameChoice.FormerMember, now, cancellationToken),
                now,
                cancellationToken);
        }

        private async Task<Result> ChangeLockedPersonAsync(
            Guid adminUserId,
            Guid personId,
            string reason,
            AdminActionType action,
            Func<AppUser, string> targetLabelOf,
            Func<AppUser, Task<bool>> change,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            await _unitOfWork.OpenTransactionAsync(cancellationToken);
            AppUser? found = await LockUserAsync(personId, cancellationToken);
            Result<AppUser> person = await PersonToActOnAsync(adminUserId, personId, found, cancellationToken);
            if (!person.IsSuccess)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                return person;
            }

            string targetLabel = targetLabelOf(person.Value);
            if (await change(person.Value))
            {
                _context.AdminActions.Add(AdminAction.AboutPerson(adminUserId, action, personId, targetLabel, reason, now));
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Ok();
        }

        private async Task<Result<AppUser>> PersonToActOnAsync(Guid adminUserId, Guid personId, AppUser? found, CancellationToken cancellationToken)
        {
            if (found is null)
            {
                return Result.NotFound<AppUser>($"No person has the id {personId}.", ResultCodes.OWNER_PERSON_NOT_FOUND);
            }

            if (personId == adminUserId || await IsAdminAsync(personId, cancellationToken))
            {
                return Result.Forbid<AppUser>($"Person {personId} is an admin of Kvit, and the owner tools never act on an admin.", ResultCodes.OWNER_CANNOT_ACT_ON_ADMIN);
            }

            return Result.Ok(found);
        }

        private static string TargetLabelOf(AppUser person)
        {
            return person.PurgedAt is null ? person.DisplayName : AdminAction.PurgedPersonLabel;
        }

        private Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken)
        {
            return _context.UserRoles.AnyAsync(
                userRole => userRole.UserId == userId && _context.Roles.Any(role => role.Id == userRole.RoleId && role.Name == Roles.Admin),
                cancellationToken);
        }

        private async Task<AppUser?> LockUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            List<AppUser> users = await _context.Users
                .FromSql($"SELECT * FROM users WHERE id = {userId} FOR UPDATE")
                .ToListAsync(cancellationToken);

            return users.SingleOrDefault();
        }
    }
}
