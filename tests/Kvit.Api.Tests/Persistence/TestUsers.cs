using Kvit.Infrastructure.Auth;

namespace Kvit.Api.Tests.Persistence
{
    public static class TestUsers
    {
        public static AppUser Create()
        {
            string email = $"user-{Guid.NewGuid():N}@example.com";

            return new AppUser
            {
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                DisplayName = "Ana",
                Language = "en",
                TimeZone = "Europe/Skopje",
                IsTimeZoneManual = false,
                CreatedAt = DateTimeOffset.UtcNow,
            };
        }
    }
}
