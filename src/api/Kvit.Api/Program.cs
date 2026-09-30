using Kvit.Api.Proxy;
using Kvit.Api.Registers;
using Kvit.Api.Settings;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddAuth();
builder.Services.AddDataProtectionKeys(DataProtectionSetting.LoadCertificate(builder.Configuration));
builder.Services.AddRateLimits();

WebApplication app = builder.Build();

KvitDatabaseSetting.Read(app.Configuration);
string? proxySecret = ProxySetting.Read(app.Configuration, app.Environment);

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
