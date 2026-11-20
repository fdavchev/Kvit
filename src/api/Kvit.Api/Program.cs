using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using Kvit.Api.Problems;
using Kvit.Api.Proxy;
using Kvit.Api.Registers;
using Kvit.Api.Settings;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<UnexpectedExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddApplication();
builder.Services.AddDomainServices();
builder.Services.AddInfrastructure();
builder.Services.AddAuth();
X509Certificate2 dataProtectionCertificate = DataProtectionSetting.LoadCertificate(builder.Configuration, TimeProvider.System.GetUtcNow());
builder.Services.AddDataProtectionKeys(dataProtectionCertificate);
builder.Services.AddRateLimits();

WebApplication app = builder.Build();

DataProtectionSetting.WarnIfCertificateExpiresSoon(dataProtectionCertificate, app.Logger, TimeProvider.System.GetUtcNow());

KvitDatabaseSetting.Read(app.Configuration);
GoogleSetting.Read(app.Configuration);
WebsiteSetting.ReadOrigin(app.Configuration);
string? proxySecret = ProxySetting.Read(app.Configuration, app.Environment);

app.UseExceptionHandler();

if (proxySecret is not null)
{
    app.UseMiddleware<ProxyGateMiddleware>(proxySecret);

    ForwardedHeadersOptions visitorAddressForwarding = new()
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor,
        ForwardedForHeaderName = ProxyGateMiddleware.VisitorAddressHeader,
        ForwardLimit = 1,
    };
    visitorAddressForwarding.KnownIPNetworks.Clear();
    visitorAddressForwarding.KnownProxies.Clear();
    app.UseForwardedHeaders(visitorAddressForwarding);
}

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.MapHealthChecks("/health").AllowAnonymous();
app.MapHealthChecks("/api/health").AllowAnonymous();
app.MapControllers();

app.Run();
