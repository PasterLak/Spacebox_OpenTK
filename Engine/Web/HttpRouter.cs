namespace Engine.Web;

public sealed class HttpRouter
{
    private readonly Dictionary<(string Method, string Path), Func<HttpRequest, Task<HttpResponse>>> _exact = new Dictionary<(string, string), Func<HttpRequest, Task<HttpResponse>>>();
    private readonly List<(string Method, string Prefix, Func<HttpRequest, string, Task<HttpResponse>> Handler)> _prefixes = new List<(string, string, Func<HttpRequest, string, Task<HttpResponse>>)>();

    public void Map(string method, string path, Func<HttpRequest, Task<HttpResponse>> handler)
    {
        _exact[(method.ToUpperInvariant(), path)] = handler;
    }

    public void Map(string method, string path, Func<HttpRequest, HttpResponse> handler)
    {
        Map(method, path, request => Task.FromResult(handler(request)));
    }

    public void MapPrefix(string method, string prefix, Func<HttpRequest, string, Task<HttpResponse>> handler)
    {
        _prefixes.Add((method.ToUpperInvariant(), prefix, handler));
    }

    public void MapPrefix(string method, string prefix, Func<HttpRequest, string, HttpResponse> handler)
    {
        MapPrefix(method, prefix, (request, rest) => Task.FromResult(handler(request, rest)));
    }

    public async Task<HttpResponse> Handle(HttpRequest request)
    {
        if (_exact.TryGetValue((request.Method, request.Path), out var handler)) return await handler(request);

        bool pathKnown = false;
        for (int i = 0; i < _prefixes.Count; i++)
        {
            var route = _prefixes[i];
            if (!request.Path.StartsWith(route.Prefix, StringComparison.Ordinal)) continue;

            pathKnown = true;
            if (route.Method == request.Method) return await route.Handler(request, request.Path.Substring(route.Prefix.Length));
        }

        foreach (var key in _exact.Keys)
        {
            if (key.Path == request.Path) pathKnown = true;
        }

        return pathKnown ? HttpResponse.Error("Method not allowed", 405) : HttpResponse.NotFound();
    }
}
