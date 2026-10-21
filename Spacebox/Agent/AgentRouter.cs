using Engine;
using Engine.Web;

namespace Spacebox.Agent;

public sealed record AgentRoute(string Method, string Path, string Description);

public sealed class AgentRouter
{
    private readonly HttpRouter _router;
    private readonly List<AgentRoute> _routes = new List<AgentRoute>();

    public AgentRouter(HttpRouter router)
    {
        _router = router;
    }

    public IReadOnlyList<AgentRoute> Routes => _routes;

    public void Get(string path, string description, Func<HttpRequest, HttpResponse> handler) => MapMain("GET", path, description, handler);

    public void Post(string path, string description, Func<HttpRequest, HttpResponse> handler) => MapMain("POST", path, description, handler);

    public void GetAsync(string path, string description, Func<HttpRequest, Task<HttpResponse>> handler) => MapAsync("GET", path, description, handler);

    public void PostAsync(string path, string description, Func<HttpRequest, Task<HttpResponse>> handler) => MapAsync("POST", path, description, handler);

    private void MapMain(string method, string path, string description, Func<HttpRequest, HttpResponse> handler)
    {
        MapAsync(method, path, description, request => MainThread.Run(() => handler(request)));
    }

    private void MapAsync(string method, string path, string description, Func<HttpRequest, Task<HttpResponse>> handler)
    {
        _routes.Add(new AgentRoute(method, path, description));
        _router.Map(method, path, async request =>
        {
            try
            {
                return await handler(request);
            }
            catch (AgentRequestException ex)
            {
                return AgentJson.Response(new { error = ex.Message }, ex.Status);
            }
            catch (Exception ex)
            {
                Debug.Error($"[Agent] {method} {path} failed: {ex}");
                return AgentJson.Response(new { error = ex.Message }, 500);
            }
        });
    }
}
