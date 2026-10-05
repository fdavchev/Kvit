using Kvit.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.Persistence
{
    public class ExchangeRateRepositoryEmptyTableTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        [Fact]
        public async Task GetLatestAsync_OnAnEmptyTable_ThrowsInvalidOperationException()
        {
            await _database.ExecuteAsync("DELETE FROM exchange_rates");
            using IServiceScope scope = _database.CreateScope();
            IExchangeRateRepository repository = scope.ServiceProvider.GetRequiredService<IExchangeRateRepository>();

            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.GetLatestAsync(TestContext.Current.CancellationToken));
        }
    }
}
