using Engine.Web.Acme;
using Spacebox.Network.Dedicated;

namespace Spacebox.Network.Dashboard;

public enum HttpsMethod
{
    Off,
    CertificateFiles,
    DuckDns,
    Cloudflare,
    HttpPort
}

public static class HttpsMethodSelector
{
    public static string PublicAddress(ServerConfig config)
    {
        string address = config.PublicAddress?.Trim() ?? "";
        if (address.Length == 0 && !string.IsNullOrWhiteSpace(config.DuckDnsDomain)) address = DuckDnsClient.FullName(config.DuckDnsDomain);
        return Normalize(address);
    }

    public static string Normalize(string address)
    {
        string value = (address ?? "").Trim().ToLowerInvariant();
        int scheme = value.IndexOf("://", StringComparison.Ordinal);
        if (scheme >= 0) value = value.Substring(scheme + 3);
        int slash = value.IndexOf('/');
        if (slash >= 0) value = value.Substring(0, slash);
        if (!AcmeClient.IsIpAddress(value))
        {
            int colon = value.LastIndexOf(':');
            if (colon > 0 && int.TryParse(value.Substring(colon + 1), out _)) value = value.Substring(0, colon);
        }
        return value.TrimEnd('.');
    }

    public static bool IsDuckDns(string address) => address.EndsWith(DuckDnsClient.Suffix, StringComparison.OrdinalIgnoreCase);

    public static HttpsMethod Select(ServerConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.DashboardCertificateFile)) return HttpsMethod.CertificateFiles;

        string address = PublicAddress(config);
        if (!config.HttpsAutomatic || address.Length == 0) return HttpsMethod.Off;
        if (AcmeClient.IsIpAddress(address)) return HttpsMethod.HttpPort;
        if (IsDuckDns(address) && !string.IsNullOrWhiteSpace(config.DuckDnsToken)) return HttpsMethod.DuckDns;
        if (!string.IsNullOrWhiteSpace(config.CloudflareApiToken)) return HttpsMethod.Cloudflare;
        return HttpsMethod.HttpPort;
    }

    public static bool UpdatesDuckDnsIp(ServerConfig config)
    {
        return config.DuckDnsUpdateIp && IsDuckDns(PublicAddress(config)) && !string.IsNullOrWhiteSpace(config.DuckDnsToken);
    }

    public static string Describe(HttpsMethod method, ServerConfig config) => method switch
    {
        HttpsMethod.DuckDns => "DuckDNS record",
        HttpsMethod.Cloudflare => "Cloudflare record",
        HttpsMethod.HttpPort => $"check on TCP port {config.HttpsChallengePort}",
        HttpsMethod.CertificateFiles => "certificate files",
        _ => "off"
    };
}
