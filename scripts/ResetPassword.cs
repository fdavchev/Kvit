#:project ../src/api/Kvit.Api/Kvit.Api.csproj
#:property PublishAot=false

using System.Reflection;
using Kvit.Api.Registers;
using Kvit.Api.Settings;
using Kvit.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.DependencyInjection;

const string Usage =
    "Usage, from the repository root: dotnet run scripts/ResetPassword.cs -- <email>\n" +
    "The database connection string comes from the environment variable ConnectionStrings__KvitDatabase when it is set " +
    "(for example for Neon), otherwise from ConnectionStrings:KvitDatabase in the user secrets of src/api/Kvit.Api. " +
    "When both exist, the environment variable wins.";

if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
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
PasswordResetService passwordReset = scope.ServiceProvider.GetRequiredService<PasswordResetService>();

string? temporaryPassword = await passwordReset.ResetAsync(email, CancellationToken.None);
if (temporaryPassword is null)
{
    Console.Error.WriteLine($"No account has the email '{email}'. Nothing changed.");
    return 1;
}

Console.WriteLine($"The password of {email} was reset, the account was unlocked and its other sessions were signed out.");
Console.WriteLine("Temporary password (shown only this once, it is not saved anywhere):");
Console.WriteLine(temporaryPassword);
Console.WriteLine("Give it to the person. After logging in with it they must choose their own password.");
return 0;
