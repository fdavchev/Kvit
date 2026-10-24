using Kvit.Domain.MoneyRules;
using Microsoft.AspNetCore.Identity;

namespace Kvit.Infrastructure.Auth
{
    public class AppUser : IdentityUser<Guid>
    {
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

        public void SetBudgetTotalCurrency(Currency? budgetTotalCurrency)
        {
            BudgetTotalCurrency = budgetTotalCurrency;
        }
    }
}
