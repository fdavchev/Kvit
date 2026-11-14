using Kvit.Domain.MoneyRules;

namespace Kvit.Contracts.Me
{
    public sealed record ExportedProfile(
        Guid Id,
        string DisplayName,
        string? Email,
        string Language,
        string TimeZone,
        DateTimeOffset CreatedAt,
        bool IsGuest,
        bool HasPassword,
        bool GoogleLinked,
        Currency? BudgetTotalCurrency,
        string? TermsVersion,
        DateTimeOffset? TermsAcceptedAt);
}
