using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Postgres;

namespace Kvit.Api.Tests.AccountDeletion
{
    public class AccountDeletionApp : ExpensesApp
    {
        private readonly Lazy<Task<DeletedPersonScenario>> _formerMember;
        private readonly Lazy<Task<DeletedPersonScenario>> _keepName;

        public AccountDeletionApp(PostgresFixture postgres)
            : base(postgres)
        {
            _formerMember = new Lazy<Task<DeletedPersonScenario>>(() => DeletedPersonSteps.CreateAsync(this, DeletionRequests.FormerMember));
            _keepName = new Lazy<Task<DeletedPersonScenario>>(() => DeletedPersonSteps.CreateAsync(this, DeletionRequests.KeepName));
        }

        public Task<DeletedPersonScenario> FormerMemberAsync()
        {
            return _formerMember.Value;
        }

        public Task<DeletedPersonScenario> KeepNameAsync()
        {
            return _keepName.Value;
        }
    }
}
