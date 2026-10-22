using System.Text;

namespace Engine.Web;

public static class HttpProtocol
{
    public const int MaxHeaderBytes = 16 * 1024;
    public static readonly TimeSpan HeaderTimeout = TimeSpan.FromSeconds(5);
    private static readonly string[] SingleHeaders = { "Content-Length", "Host" };

    public static async Task<HttpRequest> ReadRequestAsync(Stream stream, string remoteAddress, int maxBodyBytes, CancellationToken token, bool isSecure = false)
    {
        (byte[] Header, byte[] Extra)? headerBytes;
        using (var headerTimeout = CancellationTokenSource.CreateLinkedTokenSource(token))
        {
            headerTimeout.CancelAfter(HeaderTimeout);
            headerBytes = await ReadHeaderAsync(stream, headerTimeout.Token);
        }
        if (headerBytes == null) return null;

        string headerText = Encoding.ASCII.GetString(headerBytes.Value.Header);
        var lines = headerText.Split("\r\n");
        var requestLine = lines[0].Split(' ');
        if (requestLine.Length != 3 || (requestLine[2] != "HTTP/1.1" && requestLine[2] != "HTTP/1.0")) throw new InvalidDataException("Bad request line");

        var headers = ParseHeaders(lines);
        if (headers.ContainsKey("Transfer-Encoding")) throw new InvalidDataException("Transfer-Encoding is not supported");

        int length = 0;
        if (headers.TryGetValue("Content-Length", out var lengthText) && !TryParseLength(lengthText, out length))
            throw new InvalidDataException("Bad Content-Length");
        if (length > maxBodyBytes) throw new PayloadTooLargeException();

        string method = requestLine[0].ToUpperInvariant();
        if (length > 0 && (method == "GET" || method == "HEAD")) throw new InvalidDataException("GET requests have no body");

        var body = new byte[length];
        int filled = Math.Min(length, headerBytes.Value.Extra.Length);
        Array.Copy(headerBytes.Value.Extra, body, filled);
        while (filled < length)
        {
            int read = await stream.ReadAsync(body.AsMemory(filled, length - filled), token);
            if (read == 0) throw new EndOfStreamException();
            filled += read;
        }

        SplitTarget(requestLine[1], out var path, out var query);
        return new HttpRequest
        {
            Method = method,
            Path = path,
            Query = query,
            Headers = headers,
            Body = body,
            RemoteAddress = remoteAddress,
            IsSecure = isSecure
        };
    }

    public static Dictionary<string, string> ParseHeaders(string[] lines)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < lines.Length; i++)
        {
            if (lines[i].Length == 0) continue;
            int colon = lines[i].IndexOf(':');
            if (colon <= 0 || char.IsWhiteSpace(lines[i][colon - 1]) || lines[i].IndexOf('\n') >= 0) throw new InvalidDataException("Bad header line");

            string name = lines[i].Substring(0, colon);
            string value = lines[i].Substring(colon + 1).Trim();
            if (headers.ContainsKey(name) && Array.Exists(SingleHeaders, single => string.Equals(single, name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException($"Duplicate {name} header");
            headers[name] = value;
        }
        return headers;
    }

    public static bool TryParseLength(string text, out int length)
    {
        length = 0;
        if (string.IsNullOrEmpty(text) || text.Length > 9) return false;
        foreach (char c in text)
            if (c < '0' || c > '9') return false;
        length = int.Parse(text);
        return true;
    }

    public static async Task WriteResponseAsync(Stream stream, HttpResponse response, CancellationToken token)
    {
        var builder = new StringBuilder();
        builder.Append("HTTP/1.1 ").Append(response.Status).Append(' ').Append(HttpResponse.ReasonPhrase(response.Status)).Append("\r\n");
        builder.Append("Content-Type: ").Append(response.ContentType).Append("\r\n");
        builder.Append("Content-Length: ").Append(response.Body.Length).Append("\r\n");
        builder.Append("Connection: close\r\n");
        foreach (var header in response.Headers)
            builder.Append(HeaderText(header.Key)).Append(": ").Append(HeaderText(header.Value)).Append("\r\n");
        builder.Append("\r\n");

        var head = Encoding.ASCII.GetBytes(builder.ToString());
        await stream.WriteAsync(head, token);
        if (response.Body.Length > 0) await stream.WriteAsync(response.Body, token);
        await stream.FlushAsync(token);
    }

    public static string HeaderText(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        var builder = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            if (c >= ' ' && c != (char)0x7F) builder.Append(c);
        }
        return builder.ToString();
    }

    public static void SplitTarget(string target, out string path, out Dictionary<string, string> query)
    {
        query = new Dictionary<string, string>(StringComparer.Ordinal);
        int mark = target.IndexOf('?');
        path = Uri.UnescapeDataString(mark < 0 ? target : target.Substring(0, mark));
        if (mark < 0) return;

        foreach (var pair in target.Substring(mark + 1).Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int equals = pair.IndexOf('=');
            string key = Uri.UnescapeDataString((equals < 0 ? pair : pair.Substring(0, equals)).Replace('+', ' '));
            string value = equals < 0 ? "" : Uri.UnescapeDataString(pair.Substring(equals + 1).Replace('+', ' '));
            query[key] = value;
        }
    }

    private static async Task<(byte[] Header, byte[] Extra)?> ReadHeaderAsync(Stream stream, CancellationToken token)
    {
        var buffer = new byte[4096];
        var data = new MemoryStream();

        while (true)
        {
            int read = await stream.ReadAsync(buffer, token);
            if (read == 0) return data.Length == 0 ? null : throw new EndOfStreamException();

            data.Write(buffer, 0, read);
            var bytes = data.GetBuffer();
            int end = FindHeaderEnd(bytes, (int)data.Length);
            if (end >= 0)
            {
                var header = bytes.AsSpan(0, end).ToArray();
                var extra = bytes.AsSpan(end + 4, (int)data.Length - end - 4).ToArray();
                return (header, extra);
            }

            if (data.Length > MaxHeaderBytes) throw new InvalidDataException("Header too large");
        }
    }

    private static int FindHeaderEnd(byte[] bytes, int length)
    {
        for (int i = 0; i + 3 < length; i++)
        {
            if (bytes[i] == '\r' && bytes[i + 1] == '\n' && bytes[i + 2] == '\r' && bytes[i + 3] == '\n') return i;
        }
        return -1;
    }
}

public sealed class PayloadTooLargeException : Exception
{
}
