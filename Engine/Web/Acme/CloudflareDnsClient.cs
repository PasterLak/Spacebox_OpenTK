using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

namespace Engine.Web.Acme;

public sealed class CloudflareDnsClient : IDnsChallenge
{
    public const string DefaultEndpoint = "https://api.cloudflare.com/client/v4";
    public const string ChallengePrefix = "_acme-challenge.";

    private readonly HttpClient _http;
    private readonly string _token;
    private readonly string _endpoint;
    private readonly List<(string Zone, string Record)> _created = new List<(string, string)>();

    public CloudflareDnsClient(HttpClient http, string apiToken, string endpoint = DefaultEndpoint)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _token = apiToken?.Trim() ?? "";
        _endpoint = endpoint.TrimEnd('/');
    }

    public async Task SetTxtAsync(string domain, string value, CancellationToken token)
    {
        string zone = await FindZone(domain, token);
        var body = await Send(HttpMethod.Post, $"/zones/{zone}/dns_records", new JsonObject
        {
            ["type"] = "TXT",
            ["name"] = ChallengePrefix + domain,
            ["content"] = value,
            ["ttl"] = 60
        }, token);

        string record = body["result"]?["id"]?.GetValue<string>() ?? throw new AcmeException("Cloudflare did not return the record id");
        lock (_created) _created.Add((zone, record));
    }

    public async Task ClearTxtAsync(string domain, CancellationToken token)
    {
        List<(string Zone, string Record)> created;
        lock (_created)
        {
            created = new List<(string, string)>(_created);
            _created.Clear();
        }

        foreach (var (zone, record) in created)
            await Send(HttpMethod.Delete, $"/zones/{zone}/dns_records/{record}", null, token);
    }

    public static IEnumerable<string> ZoneCandidates(string domain)
    {
        var labels = domain.Trim().TrimEnd('.').ToLowerInvariant().Split('.');
        for (int i = 0; i + 2 <= labels.Length; i++)
            yield return string.Join('.', labels, i, labels.Length - i);
    }

    private async Task<string> FindZone(string domain, CancellationToken token)
    {
        foreach (var candidate in ZoneCandidates(domain))
        {
            var body = await Send(HttpMethod.Get, "/zones?name=" + Uri.EscapeDataString(candidate), null, token);
            var zones = body["result"]?.AsArray();
            if (zones != null && zones.Count > 0) return zones[0]["id"].GetValue<string>();
        }

        throw new AcmeException($"Cloudflare has no zone for {domain}, check that the domain is in this Cloudflare account and the token may edit its DNS");
    }

    private async Task<JsonObject> Send(HttpMethod method, string path, JsonObject payload, CancellationToken token)
    {
        using var request = new HttpRequestMessage(method, _endpoint + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        if (payload != null) request.Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request, token);
        string text = await response.Content.ReadAsStringAsync(token);
        JsonObject body;
        try
        {
            body = JsonNode.Parse(text) as JsonObject ?? new JsonObject();
        }
        catch (System.Text.Json.JsonException)
        {
            body = new JsonObject();
        }

        if (!response.IsSuccessStatusCode || body["success"]?.GetValue<bool>() == false)
        {
            string message = body["errors"]?.AsArray().FirstOrDefault()?["message"]?.GetValue<string>() ?? ((int)response.StatusCode).ToString();
            throw new AcmeException("Cloudflare: " + message);
        }

        return body;
    }
}
