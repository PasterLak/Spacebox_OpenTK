namespace Engine.Web;

public static class LocalRequestGuard
{
    private static readonly string[] LocalHosts = { "127.0.0.1", "localhost", "[::1]" };

    public static bool IsLocalTool(HttpRequest request)
    {
        if (!string.IsNullOrEmpty(request.Header("Origin"))) return false;

        string host = request.Host;
        int colon = host.LastIndexOf(':');
        if (colon > 0 && !host.EndsWith(']')) host = host.Substring(0, colon);
        return Array.Exists(LocalHosts, local => string.Equals(local, host, StringComparison.OrdinalIgnoreCase));
    }

    public static Func<HttpRequest, Task<HttpResponse>> Wrap(Func<HttpRequest, Task<HttpResponse>> handler)
    {
        return request => IsLocalTool(request) ? handler(request) : Task.FromResult(HttpResponse.Error("Only local tools may use this API", 403));
    }
}
