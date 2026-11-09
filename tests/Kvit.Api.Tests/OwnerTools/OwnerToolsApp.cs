using Kvit.Api.Tests.Auth;
using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Groups;
using Kvit.Api.Tests.Postgres;

namespace Kvit.Api.Tests.OwnerTools
{
    public class OwnerToolsApp : ExpensesApp
    {
        public const string AdminName = "Owner of Kvit";

        private readonly Lazy<Task<SignedInUser>> _sharedAdmin;

        public OwnerToolsApp(PostgresFixture postgres)
            : base(postgres)
        {
            _sharedAdmin = new Lazy<Task<SignedInUser>>(() => SignInAdminAsync());
        }

        public Task<SignedInUser> SharedAdminAsync()
        {
            return _sharedAdmin.Value;
        }

        public async Task<SignedInUser> SignInAdminAsync(string displayName = AdminName)
        {
            RegistrationForm form = RegistrationForm.Valid() with { DisplayName = displayName };
            Guid userId = await CreateAccountAsync(form);
            await MakeAdminAsync(form.Email);
            HttpClient client = await CreateLoggedInClientAsync(form.Email, form.Password);

            return new SignedInUser(client, userId, form);
        }

        public async Task<SignedInUser> SignUpSuspendedAsync(string displayName = GroupsApp.OwnerName)
        {
            SignedInUser person = await SignUpAsync(displayName);
            await OwnerToolsRows.SetSuspendedAtAsync(this, person.UserId, Clock.GetUtcNow());
            Clock.Advance(TimeSpan.FromSeconds(1));

            return person;
        }
    }
}
