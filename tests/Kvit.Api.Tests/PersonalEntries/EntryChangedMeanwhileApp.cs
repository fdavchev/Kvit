using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Postgres;
using Kvit.Contracts.Persistence;
using Kvit.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kvit.Api.Tests.PersonalEntries
{
    public class EntryChangedMeanwhileApp : ExpensesApp
    {
        public EntryChangedMeanwhileApp(PostgresFixture postgres)
            : base(postgres)
        {
            Action<IServiceCollection>? expenseServices = _factory.AdditionalTestServices;
            _factory.AdditionalTestServices = services =>
            {
                expenseServices?.Invoke(services);
                services.RemoveAll<IUnitOfWork>();
                services.AddScoped<IUnitOfWork>(provider => new ChangingUnitOfWork(ActivatorUtilities.CreateInstance<UnitOfWork>(provider), ChangeAfterNextTransactionOpens));
            };
        }

        public ChangeAfterNextTransactionOpens ChangeAfterNextTransactionOpens { get; } = new();
    }
}
