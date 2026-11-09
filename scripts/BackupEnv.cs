#:property PublishAot=false

using System.Data.Common;
using System.Globalization;
using System.Text;

const string ConnectionStringVariable = "NEON_DIRECT_CONNECTION_STRING";
const int DefaultPort = 5432;
const string DefaultSslMode = "require";
const int UsageErrorExitCode = 2;

Dictionary<string, string> sslModes = new(StringComparer.OrdinalIgnoreCase)
{
    ["VerifyFull"] = "verify-full",
    ["VerifyCA"] = "verify-ca",
    ["Require"] = "require",
    ["Prefer"] = "prefer",
    ["Allow"] = "allow",
    ["Disable"] = "disable",
};
Dictionary<string, string> channelBindings = new(StringComparer.OrdinalIgnoreCase)
{
    ["Require"] = "require",
    ["Prefer"] = "prefer",
    ["Disable"] = "disable",
};

string? connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable);
if (string.IsNullOrWhiteSpace(connectionString))
{
    return Fail($"The environment variable {ConnectionStringVariable} is missing or empty.");
}

DbConnectionStringBuilder builder;
try
{
    builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
}
catch (ArgumentException)
{
    return Fail($"{ConnectionStringVariable} is not a connection string of the form Key=Value;Key=Value.");
}

string? host = ValueOf(builder, "Host", "Server");
string? database = ValueOf(builder, "Database", "Initial Catalog", "DB");
string? username = ValueOf(builder, "Username", "User Name", "User Id", "UserId", "User", "UID");
string? password = ValueOf(builder, "Password", "Pwd", "Psw");
string? portText = ValueOf(builder, "Port");
string? sslModeText = ValueOf(builder, "SSL Mode", "SslMode");
string? channelBindingText = ValueOf(builder, "Channel Binding", "ChannelBinding");

if (host is null)
{
    return Fail($"{ConnectionStringVariable} has no Host.");
}

if (host.Contains("-pooler", StringComparison.OrdinalIgnoreCase))
{
    return Fail($"{ConnectionStringVariable} points at Neon's pooler (a host with -pooler); pg_dump needs the direct connection string, the one without -pooler.");
}

if (database is null)
{
    return Fail($"{ConnectionStringVariable} has no Database.");
}

if (username is null)
{
    return Fail($"{ConnectionStringVariable} has no Username.");
}

if (password is null)
{
    return Fail($"{ConnectionStringVariable} has no Password.");
}

int port = DefaultPort;
if (portText is not null && (!int.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out port) || port is < 1 or > 65535))
{
    return Fail($"{ConnectionStringVariable} has a Port that is not a whole number from 1 to 65535.");
}

string? sslMode = DefaultSslMode;
if (sslModeText is not null && !sslModes.TryGetValue(sslModeText, out sslMode))
{
    return Fail($"{ConnectionStringVariable} has the SSL Mode '{sslModeText}', which is not one of {string.Join(", ", sslModes.Keys)}.");
}

string? channelBinding = null;
if (channelBindingText is not null && !channelBindings.TryGetValue(channelBindingText, out channelBinding))
{
    return Fail($"{ConnectionStringVariable} has the Channel Binding '{channelBindingText}', which is not one of {string.Join(", ", channelBindings.Keys)}.");
}

StringBuilder exports = new();
AppendExport(exports, "PGHOST", host);
AppendExport(exports, "PGPORT", port.ToString(CultureInfo.InvariantCulture));
AppendExport(exports, "PGDATABASE", database);
AppendExport(exports, "PGUSER", username);
AppendExport(exports, "PGPASSWORD", password);
AppendExport(exports, "PGSSLMODE", sslMode);
if (channelBinding is not null)
{
    AppendExport(exports, "PGCHANNELBINDING", channelBinding);
}

Console.Out.Write(exports.ToString());
return 0;

static string? ValueOf(DbConnectionStringBuilder builder, params string[] keys)
{
    foreach (string key in keys)
    {
        if (builder.TryGetValue(key, out object? value) && value is string text && text.Length > 0)
        {
            return text;
        }
    }

    return null;
}

static void AppendExport(StringBuilder exports, string name, string value)
{
    exports.Append("export ").Append(name).Append("='").Append(value.Replace("'", "'\\''", StringComparison.Ordinal)).Append("'\n");
}

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return UsageErrorExitCode;
}
