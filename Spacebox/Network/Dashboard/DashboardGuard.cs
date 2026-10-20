using System.Text.Json;
using Engine.Web;

namespace Spacebox.Network.Dashboard;

public sealed class DashboardGuard
{
    public const string RequestHeader = "X-Spacebox-Dashboard";

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    private readonly DashboardSessions _sessions;

    public DashboardGuard(DashboardSessions sessions)
    {
        _sessions = sessions;
    }

    public bool TryGetUser(HttpRequest request, out string user)
    {
        return _sessions.TryGet(request.Cookie(DashboardSessions.CookieName), out user);
    }

    public static bool HasRequestHeader(HttpRequest request)
    {
        return request.Method == "GET" || request.Header(RequestHeader) == "1";
    }

    public Func<HttpRequest, Task<HttpResponse>> Admin(Func<HttpRequest, string, Task<HttpResponse>> handler)
    {
        return async request =>
        {
            if (!TryGetUser(request, out var user)) return HttpResponse.Error("Not logged in", 401);
            if (!HasRequestHeader(request)) return HttpResponse.Error("Missing request header", 403);
            return await handler(request, user);
        };
    }

    public Func<HttpRequest, Task<HttpResponse>> Admin(Func<HttpRequest, string, HttpResponse> handler)
    {
        return Admin((request, user) => Task.FromResult(handler(request, user)));
    }

    public static T Read<T>(HttpRequest request) where T : class
    {
        try
        {
            return request.Body.Length == 0 ? null : JsonSerializer.Deserialize<T>(request.Body, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
