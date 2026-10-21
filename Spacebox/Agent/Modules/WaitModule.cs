using Engine;
using Engine.Automation;
using Engine.Web;

namespace Spacebox.Agent.Modules;

public sealed class WaitModule : IAgentModule
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    private readonly NavigationModule _navigation;

    public WaitModule(NavigationModule navigation)
    {
        _navigation = navigation;
    }

    public void Map(AgentRouter router)
    {
        router.GetAsync("/wait", "Wait ?ms=500, or until a condition: ?until=world (loaded, no loading screen) | idle (injected input done and not navigating) | nav (navigation finished) &timeout=60 (seconds)", WaitAsync);
    }

    private async Task<HttpResponse> WaitAsync(HttpRequest request)
    {
        string until = AgentQuery.Text(request, "until");
        if (until == null)
        {
            int ms = Math.Clamp(AgentQuery.Int(request, "ms", 500), 0, 120000);
            await Task.Delay(ms);
            return AgentJson.Response(new { ok = true, waitedMs = ms });
        }

        var condition = Condition(until);
        var timeout = TimeSpan.FromSeconds(Math.Clamp(AgentQuery.Float(request, "timeout", 60f), 0.1f, 600f));
        var started = DateTime.UtcNow;

        while (DateTime.UtcNow - started < timeout)
        {
            if (await MainThread.Run(condition)) return AgentJson.Response(new { ok = true, until, waitedMs = (int)(DateTime.UtcNow - started).TotalMilliseconds });
            await Task.Delay(PollInterval);
        }

        return AgentJson.Response(new { ok = false, until, error = "timeout" }, 408);
    }

    private Func<bool> Condition(string until)
    {
        return until switch
        {
            "world" => () => StateModule.IsWorldReady,
            "idle" => () => InputInjector.IsIdle && !_navigation.IsMoving,
            "nav" => () => !_navigation.IsMoving,
            _ => throw new AgentRequestException($"Unknown condition '{until}' (world, idle, nav)")
        };
    }
}
