using System.Text;

namespace Engine.Web;

public sealed class HttpRequest
{
    public string Method { get; init; } = "GET";
    public string Path { get; init; } = "/";
    public IReadOnlyDictionary<string, string> Query { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public byte[] Body { get; init; } = Array.Empty<byte>();
    public string RemoteAddress { get; init; } = "";
    public bool IsSecure { get; init; }

    public string Host => Header("Host") ?? "";

    public string BodyText => Encoding.UTF8.GetString(Body);

    public string Header(string name)
    {
        return Headers.TryGetValue(name, out var value) ? value : null;
    }

    public string QueryValue(string name)
    {
        return Query.TryGetValue(name, out var value) ? value : null;
    }

    public string Cookie(string name)
    {
        string header = Header("Cookie");
        if (string.IsNullOrEmpty(header)) return null;

        foreach (var part in header.Split(';'))
        {
            int equals = part.IndexOf('=');
            if (equals <= 0) continue;
            if (part.Substring(0, equals).Trim() == name) return part.Substring(equals + 1).Trim();
        }

        return null;
    }
}
