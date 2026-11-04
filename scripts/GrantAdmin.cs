#:project ../src/api/Kvit.Api/Kvit.Api.csproj
#:property PublishAot=false

using System.Reflection;
using Kvit.Api.Registers;
using Kvit.Api.Settings;
using Kvit.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.DependencyInjection;

const string RevokeOption = "--revoke";
const string Usage =
    "Usage, from the repository root: dotnet run scripts/GrantAdmin.cs -- <email> [--revoke]\n" +
    "Without --revoke the account with that email gets the admin role; with --revoke it loses it.\n" +
    "The database connection string comes from the environment variable ConnectionStrings__KvitDatabase when it is set " +
    "(for example for Neon), otherwise from ConnectionStrings:KvitDatabase in the user secrets of src/api/Kvit.Api. " +
    "When both exist, the environment variable wins.";

bool isRevoke = args.Length == 2 && args[1] == RevokeOption;
bool isGrant = args.Length == 1;
if ((!isGrant && !isRevoke) || string.IsNullOrWhiteSpace(args[0]) || args[0] == RevokeOption)
{
    Console.Error.WriteLine(Usage);
    return 1;
}

string email = args[0].Trim();

Assembly apiAssembly = typeof(KvitDatabaseSetting).Assembly;
if (apiAssembly.GetCustomAttribute<UserSecretsIdAttribute>() is null)
{
    throw new InvalidOperationException($"{apiAssembly.GetName().Name} has no UserSecretsId, so its user secrets cannot be found.");
}

IConfiguration configuration = new ConfigurationBuilder()
    .AddUserSecrets(apiAssembly, optional: true)
    .AddEnvironmentVariables()
    .Build();

try
{
    KvitDatabaseSetting.Read(configuration);
}
catch (InvalidOperationException exception)
{
    Console.Error.WriteLine(exception.Message);
    Console.Error.WriteLine(Usage);
    return 1;
}

ServiceCollection services = new();
services.AddSingleton(configuration);
services.AddInfrastructure();
services.AddAuth();

await using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
await using AsyncServiceScope scope = provider.CreateAsyncScope();
AdminRoleService adminRole = scope.ServiceProvider.GetRequiredService<AdminRoleService>();

bool changed = isRevoke
    ? await adminRole.RevokeAsync(email, CancellationToken.None)
    : await adminRole.GrantAsync(email, CancellationToken.None);
if (!changed)
{
    Console.Error.WriteLine($"No account has the email '{email}'. Nothing changed.");
    return 1;
}

Console.WriteLine(isRevoke
    ? $"{email} is no longer an admin. This counts from their next request; they stay logged in."
    : $"{email} is now an admin. This counts from their next request; they do not need to log in again.");
return 0;
