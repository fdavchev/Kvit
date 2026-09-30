using Kvit.Api.Registers;
using Kvit.Api.Settings;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddAuth();
builder.Services.AddDataProtectionKeys(DataProtectionSetting.LoadCertificate(builder.Configuration));

WebApplication app = builder.Build();

KvitDatabaseSetting.Read(app.Configuration);

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
