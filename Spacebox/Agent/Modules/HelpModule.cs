using Engine.Web;

namespace Spacebox.Agent.Modules;

public sealed class HelpModule : IAgentModule
{
    private readonly AgentRouter _routes;

    public HelpModule(AgentRouter routes)
    {
        _routes = routes;
    }

    public void Map(AgentRouter router)
    {
        router.GetAsync("/", "List all endpoints", _ => Task.FromResult(AgentJson.Response(new
        {
            game = "Spacebox",
            version = Engine.Application.Version,
            routes = _routes.Routes
        })));
    }
}
