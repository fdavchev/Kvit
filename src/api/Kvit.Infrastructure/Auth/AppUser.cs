using Kvit.Domain.Accounts;
using Kvit.Domain.MoneyRules;
using Microsoft.AspNetCore.Identity;

namespace Kvit.Infrastructure.Auth
{
    public class AppUser : IdentityUser<Guid>
    {
        private const string TombstoneUserNamePrefix = "deleted-";

        private static readonly DateTimeOffset LockedForeverUntil = new(3000, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public AppUser()
        {
            Id = Guid.CreateVersion7();
        }

        public string DisplayName { get; set; } = string.Empty;

        public string Language { get; set; } = string.Empty;

        public string TimeZone { get; set; } = string.Empty;

        public bool IsTimeZoneManual { get; set; }

        public string? GooglePictureUrl { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public int LockoutCount { get; set; }

        public bool MustChangePassword { get; set; }

        public bool IsGuest { get; set; }

        public Currency? BudgetTotalCurrency { get; private set; }

        public string? TermsVersion { get; private set; }

        public DateTimeOffset? TermsAcceptedAt { get; private set; }

        public DateTimeOffset? DeletionRequestedAt { get; private set; }

        public DeletionNameChoice? DeletionNameChoice { get; private set; }

        public DateTimeOffset? PurgedAt { get; private set; }

        public static string TombstoneUserNameOf(Guid userId)
        {
            return $"{TombstoneUserNamePrefix}{userId:N}";
        }

        public void SetBudgetTotalCurrency(Currency? budgetTotalCurrency)
        {
            BudgetTotalCurrency = budgetTotalCurrency;
        }

        public void AcceptTerms(string termsVersion, DateTimeOffset acceptedAt)
        {
            TermsVersion = termsVersion;
            TermsAcceptedAt = acceptedAt;
        }

        public void RequestDeletion(DeletionNameChoice nameChoice, DateTimeOffset requestedAt)
        {
            DeletionRequestedAt = requestedAt;
            DeletionNameChoice = nameChoice;
        }

        public bool CancelPendingDeletion()
        {
            if (DeletionRequestedAt is null || PurgedAt is not null)
            {
                return false;
            }

            DeletionRequestedAt = null;
            DeletionNameChoice = null;

            return true;
        }

        public void BecomeTombstone(DeletionNameChoice nameChoice, string remainingName, string normalizedUserName, DateTimeOffset purgedAt)
        {
            DeletionRequestedAt ??= purgedAt;
            DeletionNameChoice = nameChoice;
            PurgedAt = purgedAt;
            DisplayName = remainingName;
            UserName = TombstoneUserNameOf(Id);
            NormalizedUserName = normalizedUserName;
            Email = null;
            NormalizedEmail = null;
            PasswordHash = null;
            GooglePictureUrl = null;
            BudgetTotalCurrency = null;
            TermsVersion = null;
            TermsAcceptedAt = null;
            MustChangePassword = false;
            LockoutEnabled = true;
            LockoutEnd = LockedForeverUntil;
            SecurityStamp = Guid.NewGuid().ToString("N");
            ConcurrencyStamp = Guid.NewGuid().ToString();
        }
    }
}
