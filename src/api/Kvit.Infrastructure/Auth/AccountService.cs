using Kvit.Contracts.Auth;
using Kvit.Contracts.Me;
using Kvit.Domain.Accounts;
using Kvit.Domain.Entities;
using Kvit.Domain.Results;
using Kvit.Infrastructure.Persistence;
using Kvit.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Kvit.Infrastructure.Auth
{
    public sealed class AccountService(
        UserManager<AppUser> _userManager,
        SignInManager<AppUser> _signInManager,
        AppDbContext _context,
        TimeProvider _timeProvider) : IAccountService
    {
        private const string EmailTakenMessage = "This email already has an account. Log in, or use a different email.";
        private const string EmailInvalidMessage = "The email address is not valid.";
        private const string InvalidCredentialsMessage = "The email or the password is wrong.";
        private const string LockedOutMessage = "Too many wrong passwords in a row. The account is locked for a few minutes.";
        private const string NotSignedInMessage = "Nobody is signed in.";
        private const string CurrentPasswordWrongMessage = "The current password is wrong.";

        public async Task<Result<MeResponse>> CreateAccountAsync(NewAccount account, CancellationToken cancellationToken)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            AppUser user = new()
            {
                UserName = account.Email,
                Email = account.Email,
                DisplayName = account.DisplayName,
                Language = account.Language,
                TimeZone = account.TimeZone,
                IsTimeZoneManual = false,
                CreatedAt = now,
                LockoutCount = 0,
            };

            IdentityResult created;
            try
            {
                created = await _userManager.CreateAsync(user, account.Password);
            }
            catch (DbUpdateException exception) when (IsEmailUniqueViolation(exception))
            {
                return Result.Failure<MeResponse>(EmailTakenMessage, ResultCodes.AUTH_EMAIL_TAKEN);
            }

            if (!created.Succeeded)
            {
                return RegistrationFailure(created);
            }

            _context.UsageEvents.Add(UsageEvent.SignedUp(user.Id, SignUpMethod.Email, now));
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Ok(MeResponseOf(user));
        }

        public async Task<Result<MeResponse>> CheckLogInAsync(string email, string password, string timeZone, CancellationToken cancellationToken)
        {
            AppUser? user = await _userManager.FindByEmailAsync(email.Trim());
            if (user is null)
            {
                return Result.Unauthorized<MeResponse>(InvalidCredentialsMessage, ResultCodes.AUTH_INVALID_CREDENTIALS);
            }

            PasswordCheck check = await CheckPasswordAsync(user, password);
            if (check == PasswordCheck.Locked)
            {
                return Result.Forbid<MeResponse>(LockedOutMessage, ResultCodes.AUTH_LOCKED_OUT);
            }

            if (check == PasswordCheck.Wrong)
            {
                return Result.Unauthorized<MeResponse>(InvalidCredentialsMessage, ResultCodes.AUTH_INVALID_CREDENTIALS);
            }

            user.LockoutCount = 0;
            if (!user.IsTimeZoneManual)
            {
                user.TimeZone = timeZone;
            }

            await UpdateAsync(user);

            return Result.Ok(MeResponseOf(user));
        }

        public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
        {
            AppUser? user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return Result.Unauthorized(NotSignedInMessage, ResultCodes.AUTH_NOT_SIGNED_IN);
            }

            PasswordCheck check = await CheckPasswordAsync(user, currentPassword);
            if (check == PasswordCheck.Locked)
            {
                return Result.Forbid(LockedOutMessage, ResultCodes.AUTH_LOCKED_OUT);
            }

            if (check == PasswordCheck.Wrong)
            {
                return Result.Failure(CurrentPasswordWrongMessage, ResultCodes.AUTH_CURRENT_PASSWORD_WRONG);
            }

            user.LockoutCount = 0;
            user.MustChangePassword = false;
            IdentityResultChecks.ThrowIfFailed(await _userManager.ChangePasswordAsync(user, currentPassword, newPassword), $"change the password of user {user.Id}");

            return Result.Ok();
        }

        public async Task SignInAsync(Guid userId)
        {
            AppUser user = await _userManager.FindByIdAsync(userId.ToString())
                ?? throw new InvalidOperationException($"User {userId} was asked to be signed in, but no such user exists.");

            await _signInManager.SignInAsync(user, isPersistent: true);
        }

        public Task LogOutAsync()
        {
            return _signInManager.SignOutAsync();
        }

        public async Task<Result> ChangeLanguageAsync(Guid userId, string language, CancellationToken cancellationToken)
        {
            int updatedUsers = await _context.Users
                .Where(user => user.Id == userId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.Language, language), cancellationToken);

            if (updatedUsers == 0)
            {
                return Result.Unauthorized(NotSignedInMessage, ResultCodes.AUTH_NOT_SIGNED_IN);
            }

            return Result.Ok();
        }

        private async Task<PasswordCheck> CheckPasswordAsync(AppUser user, string password)
        {
            if (await _userManager.IsLockedOutAsync(user))
            {
                return PasswordCheck.Locked;
            }

            SignInResult check = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
            if (check.IsLockedOut)
            {
                user.LockoutCount++;
                user.LockoutEnd = _timeProvider.GetUtcNow() + LockoutLadder.LockDurationFor(user.LockoutCount);
                await UpdateAsync(user);
                return PasswordCheck.Locked;
            }

            if (check.IsNotAllowed || check.RequiresTwoFactor)
            {
                throw new InvalidOperationException($"Identity answered '{check}' for user {user.Id}, but Kvit turns on neither confirmed accounts nor two-factor log-in.");
            }

            return check.Succeeded ? PasswordCheck.Right : PasswordCheck.Wrong;
        }

        private async Task UpdateAsync(AppUser user)
        {
            IdentityResultChecks.ThrowIfFailed(await _userManager.UpdateAsync(user), $"update user {user.Id}");
        }

        private static bool IsEmailUniqueViolation(DbUpdateException exception)
        {
            return exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: AppUserConfiguration.NormalizedEmailIndexName or AppUserConfiguration.NormalizedUserNameIndexName,
            };
        }

        private static Result<MeResponse> RegistrationFailure(IdentityResult result)
        {
            Result<MeResponse>?[] failures = [.. result.Errors.Select(error => RegistrationFailureFor(error.Code))];
            if (failures.Any(failure => failure is null))
            {
                throw new InvalidOperationException($"Identity refused the new account with an error Kvit does not map: {IdentityResultChecks.Describe(result)}");
            }

            return failures[0]!;
        }

        private static Result<MeResponse>? RegistrationFailureFor(string identityErrorCode)
        {
            return identityErrorCode switch
            {
                nameof(IdentityErrorDescriber.DuplicateUserName) or nameof(IdentityErrorDescriber.DuplicateEmail)
                    => Result.Failure<MeResponse>(EmailTakenMessage, ResultCodes.AUTH_EMAIL_TAKEN),
                nameof(IdentityErrorDescriber.InvalidEmail) or nameof(IdentityErrorDescriber.InvalidUserName)
                    => Result.Failure<MeResponse>(EmailInvalidMessage, ResultCodes.AUTH_EMAIL_INVALID),
                _ => null,
            };
        }

        private static MeResponse MeResponseOf(AppUser user)
        {
            string email = user.Email ?? throw new InvalidOperationException($"User {user.Id} has no email.");

            return new MeResponse(user.Id, user.DisplayName, email, user.Language, user.TimeZone, user.MustChangePassword);
        }
    }
}
