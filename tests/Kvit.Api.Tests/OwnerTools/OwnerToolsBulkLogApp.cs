using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Postgres;

namespace Kvit.Api.Tests.OwnerTools
{
    public class OwnerToolsBulkLogApp : OwnerToolsApp
    {
        public const int SeededRows = 260;

        private static readonly DateTimeOffset FirstRowAt = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

        private readonly Lazy<Task<SignedInUser>> _seededAdmin;

        public OwnerToolsBulkLogApp(PostgresFixture postgres)
            : base(postgres)
        {
            _seededAdmin = new Lazy<Task<SignedInUser>>(SeedAsync);
        }

        public Task<SignedInUser> SeededAdminAsync()
        {
            return _seededAdmin.Value;
        }

        private async Task<SignedInUser> SeedAsync()
        {
            SignedInUser admin = await SignInAdminAsync();
            await OwnerToolsRows.InsertManyActionsAsync(this, admin.UserId, SeededRows, FirstRowAt);

            return admin;
        }
    }
}
