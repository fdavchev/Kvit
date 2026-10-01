using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

const string ApiKeyVariable = "RENDER_API_KEY";
const string LiveStatus = "live";
const int RequestTimeoutSeconds = 30;
const int MaximumBodyLength = 300;
const string Usage =
    "Usage, from the repository root: dotnet run scripts/RenderDeploy.cs -- --service <id> (--commit <sha> | --watch-latest) " +
    "[--base-url <url>] [--poll-seconds <number>] [--timeout-minutes <number>]\n" +
    $"The Render API key comes only from the environment variable {ApiKeyVariable}.";

string? service = null;
string? commit = null;
bool watchLatest = false;
string baseUrl = "https://api.render.com";
double pollSeconds = 10;
double timeoutMinutes = 20;
for (int index = 0; index < args.Length; index++)
{
    bool hasValue = index + 1 < args.Length;
    if (args[index] == "--watch-latest")
    {
        watchLatest = true;
    }
    else if (args[index] == "--service" && hasValue)
    {
        index++;
        service = args[index];
    }
    else if (args[index] == "--commit" && hasValue)
    {
        index++;
        commit = args[index];
    }
    else if (args[index] == "--base-url" && hasValue)
    {
        index++;
        baseUrl = args[index];
    }
    else if (args[index] == "--poll-seconds" && hasValue && TryParsePositive(args[index + 1], out pollSeconds))
    {
        index++;
    }
    else if (args[index] == "--timeout-minutes" && hasValue && TryParsePositive(args[index + 1], out timeoutMinutes))
    {
        index++;
    }
    else
    {
        Console.Error.WriteLine($"Unknown, incomplete or invalid argument: {args[index]}. {Usage}");
        return 1;
    }
}

if (string.IsNullOrWhiteSpace(service))
{
    Console.Error.WriteLine($"--service is required. {Usage}");
    return 1;
}

if (watchLatest == !string.IsNullOrWhiteSpace(commit))
{
    Console.Error.WriteLine($"Pass exactly one of --commit <sha> or --watch-latest. {Usage}");
    return 1;
}

if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? baseUri) || (baseUri.Scheme != Uri.UriSchemeHttps && baseUri.Scheme != Uri.UriSchemeHttp))
{
    Console.Error.WriteLine($"--base-url must be an absolute http or https address, not '{baseUrl}'.");
    return 1;
}

string? apiKey = Environment.GetEnvironmentVariable(ApiKeyVariable);
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine($"The environment variable {ApiKeyVariable} is missing or empty, so Render cannot be called. Nothing was sent.");
    return 1;
}

string deploysUrl = $"{baseUrl.TrimEnd('/')}/v1/services/{Uri.EscapeDataString(service)}/deploys";
TimeSpan pollInterval = TimeSpan.FromSeconds(pollSeconds);
TimeSpan timeout = TimeSpan.FromMinutes(timeoutMinutes);

using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds) };
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

try
{
    (string Id, string Status) deploy;
    if (string.IsNullOrWhiteSpace(commit))
    {
        string listUrl = $"{deploysUrl}?limit=1";
        using HttpResponseMessage listResponse = await client.GetAsync(listUrl);
        string listText = await ReadSuccessBodyAsync(listResponse, "GET", listUrl);
        deploy = ReadLatestDeploy(listText, listUrl);
    }
    else
    {
        using StringContent triggerBody = new(TriggerBodyJson(commit), Encoding.UTF8, "application/json");
        using HttpResponseMessage triggerResponse = await client.PostAsync(deploysUrl, triggerBody);
        if (triggerResponse.StatusCode == HttpStatusCode.Accepted)
        {
            Console.Error.WriteLine(
                "Render answered 202: the deploy was queued behind another deploy that is still running, so this script cannot follow it. " +
                "Wait for the running deploy to finish in the Render dashboard, then retry.");
            return 1;
        }

        string triggerText = await ReadSuccessBodyAsync(triggerResponse, "POST", deploysUrl);
        using JsonDocument triggerDocument = JsonDocument.Parse(triggerText);
        deploy = ReadDeploy(triggerDocument.RootElement, $"POST {deploysUrl}");
    }

    string deployUrl = $"{deploysUrl}/{Uri.EscapeDataString(deploy.Id)}";
    string status = deploy.Status;
    Console.WriteLine($"deploy {deploy.Id}: {status}");
    Stopwatch waited = Stopwatch.StartNew();
    while (true)
    {
        if (status == LiveStatus)
        {
            Console.WriteLine($"Deploy {deploy.Id} is live.");
            return 0;
        }

        if (IsFailed(status))
        {
            Console.Error.WriteLine($"Deploy {deploy.Id} ended with the status {status}. Open it in the Render dashboard to see its logs.");
            return 1;
        }

        if (!IsStillRunning(status))
        {
            Console.Error.WriteLine($"Deploy {deploy.Id} has the status {status}, which this script does not know, so it stopped waiting. Check it in the Render dashboard.");
            return 1;
        }

        if (waited.Elapsed >= timeout)
        {
            Console.Error.WriteLine(
                $"Timed out after {waited.Elapsed.TotalMinutes.ToString("0.##", CultureInfo.InvariantCulture)} minutes waiting for deploy {deploy.Id} to go live; " +
                $"its last status was {status}.");
            return 1;
        }

        await Task.Delay(pollInterval);
        using HttpResponseMessage pollResponse = await client.GetAsync(deployUrl);
        string pollText = await ReadSuccessBodyAsync(pollResponse, "GET", deployUrl);
        using JsonDocument pollDocument = JsonDocument.Parse(pollText);
        string nextStatus = ReadDeploy(pollDocument.RootElement, $"GET {deployUrl}").Status;
        if (nextStatus != status)
        {
            status = nextStatus;
            Console.WriteLine($"deploy {deploy.Id}: {status}");
        }
    }
}
catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException or InvalidDataException)
{
    Console.Error.WriteLine($"The Render deploy could not be followed: {exception.Message.Replace(apiKey, $"[{ApiKeyVariable}]", StringComparison.Ordinal)}");
    return 1;
}

static bool TryParsePositive(string text, out double value)
{
    return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value) && value > 0;
}

static bool IsStillRunning(string status)
{
    return status is "created" or "queued" or "build_in_progress" or "update_in_progress" or "pre_deploy_in_progress";
}

static bool IsFailed(string status)
{
    return status is "build_failed" or "update_failed" or "pre_deploy_failed" or "canceled" or "deactivated";
}

static string TriggerBodyJson(string commit)
{
    using MemoryStream json = new();
    using (Utf8JsonWriter writer = new(json))
    {
        writer.WriteStartObject();
        writer.WriteString("commitId", commit);
        writer.WriteString("clearCache", "do_not_clear");
        writer.WriteEndObject();
    }

    return Encoding.UTF8.GetString(json.ToArray());
}

static async Task<string> ReadSuccessBodyAsync(HttpResponseMessage response, string method, string url)
{
    string body = await response.Content.ReadAsStringAsync();
    if (!response.IsSuccessStatusCode)
    {
        throw new HttpRequestException($"Render answered {(int)response.StatusCode} ({response.ReasonPhrase}) to {method} {url}: {Shorten(body)}");
    }

    return body;
}

static (string Id, string Status) ReadLatestDeploy(string listText, string listUrl)
{
    using JsonDocument document = JsonDocument.Parse(listText);
    JsonElement root = document.RootElement;
    if (root.ValueKind != JsonValueKind.Array)
    {
        throw new InvalidDataException($"Render's answer to GET {listUrl} is not a list: {Shorten(listText)}");
    }

    if (root.GetArrayLength() == 0)
    {
        throw new InvalidDataException($"Render lists no deploys at GET {listUrl}, so there is nothing to watch.");
    }

    JsonElement first = root[0];
    if (first.ValueKind != JsonValueKind.Object || !first.TryGetProperty("deploy", out JsonElement deploy))
    {
        throw new InvalidDataException($"The first item in Render's answer to GET {listUrl} has no deploy: {Shorten(first.GetRawText())}");
    }

    return ReadDeploy(deploy, $"GET {listUrl}");
}

static (string Id, string Status) ReadDeploy(JsonElement deploy, string request)
{
    if (deploy.ValueKind == JsonValueKind.Object
        && deploy.TryGetProperty("id", out JsonElement id) && id.ValueKind == JsonValueKind.String && id.GetString() is string deployId
        && deploy.TryGetProperty("status", out JsonElement status) && status.ValueKind == JsonValueKind.String && status.GetString() is string deployStatus)
    {
        return (deployId, deployStatus);
    }

    throw new InvalidDataException($"Render's answer to {request} has no deploy with a text id and status: {Shorten(deploy.GetRawText())}");
}

static string Shorten(string body)
{
    string oneLine = body.Trim().ReplaceLineEndings(" ");
    return oneLine.Length > MaximumBodyLength ? $"{oneLine[..MaximumBodyLength]}..." : oneLine;
}
