using System.Net;
using Kvit.Api.Tests.Persistence;
using Kvit.Domain.ExchangeRates;
using Kvit.Domain.Interfaces;
using Kvit.Domain.Results;
using Kvit.Domain.Services.ExchangeRates;
using Kvit.Infrastructure.ExchangeRates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kvit.Api.Tests.ExchangeRates
{
    public class ExchangeRateRegistrationTests(MigratedDatabase _database) : IClassFixture<MigratedDatabase>
    {
        private const string BaseUrlSetting = "ExchangeRates:NbrmBaseUrl";

        [Fact]
        public void NbrmBaseUrl_InAppsettings_IsTheHttpsNbrmServiceAddress()
        {
            using IServiceScope scope = _database.CreateScope();
            IConfiguration configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            string baseUrl = configuration[BaseUrlSetting] ?? throw new InvalidOperationException($"{BaseUrlSetting} is not set in appsettings.json.");

            Uri address = new(baseUrl, UriKind.Absolute);
            Assert.Equal("https", address.Scheme);
            Assert.Equal("www.nbrm.mk", address.Host);
            Assert.Equal("/KLServiceNOV", address.AbsolutePath.TrimEnd('/'));
        }

        [Fact]
        public void ExchangeRateSource_FromTheContainer_IsTheNbrmSource()
        {
            using IServiceScope scope = _database.CreateScope();

            IExchangeRateSource source = scope.ServiceProvider.GetRequiredService<IExchangeRateSource>();

            Assert.IsType<NbrmExchangeRateSource>(source);
        }

        [Fact]
        public void GetCurrentExchangeRate_FromTheContainer_CanBeResolved()
        {
            using IServiceScope scope = _database.CreateScope();

            GetCurrentExchangeRate service = scope.ServiceProvider.GetRequiredService<GetCurrentExchangeRate>();

            Assert.NotNull(service);
        }

        [Fact]
        public void ExchangeRateRepository_FromTheContainer_CanBeResolved()
        {
            using IServiceScope scope = _database.CreateScope();

            IExchangeRateRepository repository = scope.ServiceProvider.GetRequiredService<IExchangeRateRepository>();

            Assert.NotNull(repository);
        }

        [Fact]
        public void RefreshGate_InTwoScopes_IsTheSameInstanceSoAFailureIsRemembered()
        {
            using IServiceScope first = _database.CreateScope();
            using IServiceScope second = _database.CreateScope();

            ExchangeRateRefreshGate firstGate = first.ServiceProvider.GetRequiredService<ExchangeRateRefreshGate>();
            ExchangeRateRefreshGate secondGate = second.ServiceProvider.GetRequiredService<ExchangeRateRefreshGate>();

            Assert.Same(firstGate, secondGate);
        }

        [Fact]
        public async Task ExchangeRateSource_WithAConfiguredBaseUrl_AsksThatAddressAndNeverTheRealNbrm()
        {
            StubHttpMessageHandler handler = StubHttpMessageHandler.Answering(HttpStatusCode.OK, NbrmAnswers.List(NbrmAnswers.RawEurExample));
            using WebApplicationFactory<Program> configured = _database.CreateAnotherInstance().WithWebHostBuilder(builder =>
            {
                builder.UseSetting(BaseUrlSetting, "https://configured.test/Service/");
                builder.ConfigureTestServices(services =>
                    services.ConfigureHttpClientDefaults(defaults => defaults.ConfigurePrimaryHttpMessageHandler(() => handler)));
            });
            using IServiceScope scope = configured.Services.CreateScope();
            IExchangeRateSource source = scope.ServiceProvider.GetRequiredService<IExchangeRateSource>();

            Result<FetchedExchangeRate> result = await source.FetchEurRateAsync(new DateOnly(2026, 9, 24), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            StubRequest request = Assert.Single(handler.Requests);
            Assert.Equal(
                "https://configured.test/Service/GetExchangeRate?StartDate=24.09.2026&EndDate=24.09.2026&format=json",
                request.Uri?.AbsoluteUri);
        }
    }
}
