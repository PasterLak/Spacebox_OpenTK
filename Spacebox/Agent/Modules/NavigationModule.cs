using Engine.Web;
using Spacebox.Agent.Navigation;

namespace Spacebox.Agent.Modules;

public sealed class NavigationModule : IAgentModule, IAgentTicker
{
    private readonly AgentNavigator _navigator = new AgentNavigator();

    public object Status => _navigator.Status;
    public bool IsMoving => _navigator.IsMoving;

    public void Map(AgentRouter router)
    {
        router.Post("/goto", "Fly to a world point along an A* path through free cells: ?x=&y=&z=&speed=8&face=1 (a solid goal ends at the nearest free cell)", request =>
        {
            var player = AgentGame.RequirePlayer();
            _navigator.Start(player, AgentQuery.Vector(request), AgentQuery.Float(request, "speed", 8f), AgentQuery.Bool(request, "face", true));
            return AgentJson.Response(_navigator.Status);
        });

        router.Get("/goto", "Navigation status: idle, moving, arrived, partial, failed, stopped", _ => AgentJson.Response(_navigator.Status));

        router.Post("/goto/stop", "Stop navigation", _ =>
        {
            _navigator.Stop("stopped by request");
            return AgentJson.Response(_navigator.Status);
        });
    }

    public void Tick() => _navigator.Tick();
}
