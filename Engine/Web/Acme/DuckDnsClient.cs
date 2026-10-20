namespace Engine.Web.Acme;

public sealed class DuckDnsClient : IDnsChallenge
{
    public const string DefaultEndpoint = "https://www.duckdns.org/update";
    public const string Suffix = ".duckdns.org";

    private readonly HttpClient _http;
    private readonly string _token;
    private readonly string _endpoint;

    public DuckDnsClient(HttpClient http, string token, string endpoint = DefaultEndpoint)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _token = token?.Trim() ?? "";
        _endpoint = endpoint;
    }

    public static string SubDomain(string domain)
    {
        string name = (domain ?? "").Trim().ToLowerInvariant();
        if (name.EndsWith(Suffix)) name = name.Substring(0, name.Length - Suffix.Length);
        int dot = name.LastIndexOf('.');
        return dot >= 0 ? name.Substring(dot + 1) : name;
    }

    public static string FullName(string domain) => SubDomain(domain) + Suffix;

    public string UpdateUrl(string domain, string query)
    {
        return $"{_endpoint}?domains={Uri.EscapeDataString(SubDomain(domain))}&token={Uri.EscapeDataString(_token)}&{query}";
    }

    public Task UpdateIpAsync(string domain, CancellationToken token) => Call(UpdateUrl(domain, "ip="), token);

    public Task SetTxtAsync(string domain, string value, CancellationToken token) => Call(UpdateUrl(domain, "txt=" + Uri.EscapeDataString(value)), token);

    public Task ClearTxtAsync(string domain, CancellationToken token) => Call(UpdateUrl(domain, "txt=&clear=true"), token);

    private async Task Call(string url, CancellationToken token)
    {
        string answer = (await _http.GetStringAsync(url, token)).Trim();
        if (!answer.StartsWith("OK", StringComparison.Ordinal))
            throw new InvalidOperationException("DuckDNS refused the update, check the domain and token");
    }
}
