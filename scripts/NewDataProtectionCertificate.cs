using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

const string CertificateBase64Key = "DataProtection:CertificateBase64";
const string CertificatePasswordKey = "DataProtection:CertificatePassword";
const string Usage = "Usage: dotnet run scripts/NewDataProtectionCertificate.cs -- [--project <path>] [--force]";

string project = "src/api/Kvit.Api";
bool force = false;
for (int index = 0; index < args.Length; index++)
{
    if (args[index] == "--force")
    {
        force = true;
    }
    else if (args[index] == "--project" && index + 1 < args.Length)
    {
        index++;
        project = args[index];
    }
    else
    {
        Console.Error.WriteLine($"Unknown or incomplete argument: {args[index]}. {Usage}");
        return 1;
    }
}

(int listExitCode, string listOutput, string listError) = RunUserSecrets(["list", "--project", project], string.Empty);
if (listExitCode != 0)
{
    ReportFailure("list", project, listOutput, listError);
    return 1;
}

bool hasCertificate = HasSecret(listOutput, CertificateBase64Key);
bool hasPassword = HasSecret(listOutput, CertificatePasswordKey);
if (hasCertificate && hasPassword && !force)
{
    Console.WriteLine(
        $"Nothing changed: {project} already has {CertificateBase64Key} and {CertificatePasswordKey} in its user secrets. " +
        "Replacing the certificate makes every stored login key unreadable and logs everyone out. " +
        $"To replace it anyway, run: dotnet run scripts/NewDataProtectionCertificate.cs -- --project {project} --force");
    return 0;
}

if (hasCertificate != hasPassword)
{
    string missing = hasCertificate ? CertificatePasswordKey : CertificateBase64Key;
    Console.WriteLine($"{project} has only one of the two settings ({missing} is missing), so the certificate could not be opened anyway. Both are being replaced.");
}

string password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
DateTimeOffset now = DateTimeOffset.UtcNow;
using RSA key = RSA.Create(2048);
CertificateRequest request = new("CN=Kvit Data Protection", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
using X509Certificate2 certificate = request.CreateSelfSigned(now.AddDays(-1), now.AddYears(10));
byte[] pfx = certificate.ExportPkcs12(Pkcs12ExportPbeParameters.Pbes2Aes256Sha256, password);

using MemoryStream secretsJson = new();
using (Utf8JsonWriter writer = new(secretsJson))
{
    writer.WriteStartObject();
    writer.WriteString(CertificateBase64Key, Convert.ToBase64String(pfx));
    writer.WriteString(CertificatePasswordKey, password);
    writer.WriteEndObject();
}

(int setExitCode, string setOutput, string setError) = RunUserSecrets(["set", "--project", project], Encoding.UTF8.GetString(secretsJson.ToArray()));
if (setExitCode != 0)
{
    ReportFailure("set", project, setOutput, setError);
    return 1;
}

Console.WriteLine($"Saved {CertificateBase64Key} and {CertificatePasswordKey} to the user secrets of {project}.");
Console.WriteLine($"The certificate expires on {certificate.NotAfter.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.");
return 0;

static (int ExitCode, string Output, string Error) RunUserSecrets(string[] arguments, string standardInput)
{
    ProcessStartInfo startInfo = new("dotnet")
    {
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        StandardInputEncoding = new UTF8Encoding(false),
        UseShellExecute = false,
    };
    startInfo.ArgumentList.Add("user-secrets");
    foreach (string argument in arguments)
    {
        startInfo.ArgumentList.Add(argument);
    }

    using Process process = Process.Start(startInfo)
        ?? throw new InvalidOperationException("Could not start 'dotnet user-secrets'. Is the .NET SDK on the PATH?");
    Task<string> output = process.StandardOutput.ReadToEndAsync();
    Task<string> error = process.StandardError.ReadToEndAsync();
    process.StandardInput.Write(standardInput);
    process.StandardInput.Close();
    process.WaitForExit();

    return (process.ExitCode, output.Result, error.Result);
}

static bool HasSecret(string listOutput, string key)
{
    string prefix = $"{key} =";
    return listOutput
        .Split('\n')
        .Select(line => line.Trim())
        .Any(line => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(line[prefix.Length..]));
}

static void ReportFailure(string command, string project, string output, string error)
{
    Console.Error.WriteLine($"'dotnet user-secrets {command} --project {project}' failed:");
    Console.Error.WriteLine(string.IsNullOrWhiteSpace(error) ? output.Trim() : error.Trim());
    Console.Error.WriteLine("Run the script from the repository root (the folder that contains Kvit.slnx), or pass --project <path>.");
}
