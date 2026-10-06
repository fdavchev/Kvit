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
        private const string UsesGoogleMessage = "This account signs in with Google and has no password.";
        private const string PasswordAlreadySetMessage = "This account already has a password. Use change password instead.";
        private const string GoogleNoAccountMessage = "No account is linked to this Google account yet.";
        private const string GoogleLoginProvider = "Google";

        private static readonly Result<MeResponse> EmailTaken = Result.Failure<MeResponse>(EmailTakenMessage, ResultCodes.AUTH_EMAIL_TAKEN);
        private static readonly Result<MeResponse> GoogleEmailTaken = Result.Failure<MeResponse>(
            "This email already has an account. Log in with your password.", ResultCodes.AUTH_GOOGLE_EMAIL_TAKEN);

        public async Task<Result<MeResponse>> CreateAccountAsync(NewAccount account, CancellationToken cancellationToken)
        {
            AppUser user = NewUser(account.DisplayName, account.Email, account.TimeZone, account.Language);

            IdentityResult created;
            try
            {
                created = await _userManager.CreateAsync(user, account.Password);
            }
            catch (DbUpdateException exception) when (IsEmailUniqueViolation(exception))
            {
                return EmailTaken;
            }

            if (!created.Succeeded)
            {
                return RegistrationFailure(created, EmailTaken);
            }

            await RecordSignUpAsync(user, SignUpMethod.Email, cancellationToken);

            return Result.Ok(MeResponseOf(user));
        }

        public async Task<Result<MeResponse>> CreateGoogleAccountAsync(NewGoogleAccount account, string googleSubject, string? googlePictureUrl, CancellationToken cancellationToken)
        {
            AppUser? linkedUser = await _userManager.FindByLoginAsync(GoogleLoginProvider, googleSubject);
            if (linkedUser is not null)
            {
                return Result.Ok(MeResponseOf(linkedUser));
            }

            AppUser user = NewUser(account.DisplayName, account.Email, account.TimeZone, account.Language);
            user.GooglePictureUrl = googlePictureUrl;

            IdentityResult created;
            try
            {
                created = await _userManager.CreateAsync(user);
            }
            catch (DbUpdateException exception) when (IsEmailUniqueViolation(exception))
            {
                return GoogleEmailTaken;
            }

            if (!created.Succeeded)
            {
                return RegistrationFailure(created, GoogleEmailTaken);
            }

            UserLoginInfo googleLogin = new(GoogleLoginProvider, googleSubject, GoogleLoginProvider);
            IdentityResultChecks.ThrowIfFailed(await _userManager.AddLoginAsync(user, googleLogin), $"link the Google account to user {user.Id}");
            await RecordSignUpAsync(user, SignUpMethod.Google, cancellationToken);

            return Result.Ok(MeResponseOf(user));
        }

        public async Task<Result<MeResponse>> CheckLogInAsync(string email, string password, string timeZone, CancellationToken cancellationToken)
        {
            AppUser? user = await _userManager.FindByEmailAsync(email.Trim());
            if (user is null)
            {
                return Result.Unauthorized<MeResponse>(InvalidCredentialsMessage, ResultCodes.AUTH_INVALID_CREDENTIALS);
            }

            if (user.PasswordHash is null)
            {
                return Result.Failure<MeResponse>(UsesGoogleMessage, ResultCodes.AUTH_USES_GOOGLE);
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

        public async Task<Result<MeResponse>> CheckGoogleLogInAsync(GoogleIdentity identity, string timeZone, CancellationToken cancellationToken)
        {
            AppUser? user = await _userManager.FindByLoginAsync(GoogleLoginProvider, identity.Subject);
            if (user is null)
            {
                AppUser? sameEmailUser = await _userManager.FindByEmailAsync(identity.Email);
                if (sameEmailUser is not null)
                {
                    return GoogleEmailTaken;
                }

                return Result.NotFound<MeResponse>(GoogleNoAccountMessage, ResultCodes.AUTH_GOOGLE_NO_ACCOUNT);
            }

            if (!user.IsTimeZoneManual)
            {
                user.TimeZone = timeZone;
            }

            user.GooglePictureUrl = identity.PictureUrl;
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

            if (user.PasswordHash is null)
            {
                return Result.Failure(UsesGoogleMessage, ResultCodes.AUTH_USES_GOOGLE);
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

        public async Task<Result> SetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken)
        {
            AppUser? user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return Result.Unauthorized(NotSignedInMessage, ResultCodes.AUTH_NOT_SIGNED_IN);
            }

            if (user.PasswordHash is not null)
            {
                return Result.Failure(PasswordAlreadySetMessage, ResultCodes.AUTH_PASSWORD_ALREADY_SET);
            }

            IdentityResultChecks.ThrowIfFailed(await _userManager.AddPasswordAsync(user, newPassword), $"set the first password of user {user.Id}");

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

        private AppUser NewUser(string displayName, string email, string timeZone, string language)
        {
            return new AppUser
            {
                UserName = email,
                Email = email,
                DisplayName = displayName,
                Language = language,
                TimeZone = timeZone,
                IsTimeZoneManual = false,
                CreatedAt = _timeProvider.GetUtcNow(),
                LockoutCount = 0,
            };
        }

        private async Task RecordSignUpAsync(AppUser user, SignUpMethod method, CancellationToken cancellationToken)
        {
            _context.UsageEvents.Add(UsageEvent.SignedUp(user.Id, method, user.CreatedAt));
            await _context.SaveChangesAsync(cancellationToken);
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

        private static Result<MeResponse> RegistrationFailure(IdentityResult result, Result<MeResponse> emailTaken)
        {
            Result<MeResponse>?[] failures = [.. result.Errors.Select(error => RegistrationFailureFor(error.Code, emailTaken))];
            if (failures.Any(failure => failure is null))
            {
                throw new InvalidOperationException($"Identity refused the new account with an error Kvit does not map: {IdentityResultChecks.Describe(result)}");
            }

            return failures[0]!;
        }

        private static Result<MeResponse>? RegistrationFailureFor(string identityErrorCode, Result<MeResponse> emailTaken)
        {
            return identityErrorCode switch
            {
                nameof(IdentityErrorDescriber.DuplicateUserName) or nameof(IdentityErrorDescriber.DuplicateEmail)
                    => emailTaken,
                nameof(IdentityErrorDescriber.InvalidEmail) or nameof(IdentityErrorDescriber.InvalidUserName)
                    => Result.Failure<MeResponse>(EmailInvalidMessage, ResultCodes.AUTH_EMAIL_INVALID),
                _ => null,
            };
        }

        private static MeResponse MeResponseOf(AppUser user)
        {
            string email = user.Email ?? throw new InvalidOperationException($"User {user.Id} has no email.");

            return new MeResponse(user.Id, user.DisplayName, email, user.Language, user.TimeZone, user.MustChangePassword, user.PasswordHash is not null, user.GooglePictureUrl);
        }
    }
}
