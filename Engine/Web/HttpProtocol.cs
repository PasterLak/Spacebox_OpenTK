using System.Text;

namespace Engine.Web;

public static class HttpProtocol
{
    public const int MaxHeaderBytes = 16 * 1024;

    public static async Task<HttpRequest> ReadRequestAsync(Stream stream, string remoteAddress, int maxBodyBytes, CancellationToken token, bool isSecure = false)
    {
        var headerBytes = await ReadHeaderAsync(stream, token);
        if (headerBytes == null) return null;

        string headerText = Encoding.ASCII.GetString(headerBytes.Value.Header);
        var lines = headerText.Split("\r\n");
        var requestLine = lines[0].Split(' ');
        if (requestLine.Length < 3) throw new InvalidDataException("Bad request line");

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < lines.Length; i++)
        {
            int colon = lines[i].IndexOf(':');
            if (colon <= 0) continue;
            headers[lines[i].Substring(0, colon).Trim()] = lines[i].Substring(colon + 1).Trim();
        }

        int length = 0;
        if (headers.TryGetValue("Content-Length", out var lengthText) && (!int.TryParse(lengthText, out length) || length < 0))
            throw new InvalidDataException("Bad Content-Length");
        if (length > maxBodyBytes) throw new PayloadTooLargeException();

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
            Method = requestLine[0].ToUpperInvariant(),
            Path = path,
            Query = query,
            Headers = headers,
            Body = body,
            RemoteAddress = remoteAddress,
            IsSecure = isSecure
        };
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
