using Engine;
using Engine.Web;

namespace Spacebox.Agent.Modules;

public sealed class AppModule : IAgentModule
{
    public void Map(AgentRouter router)
    {
        router.Post("/quit", "Close the game without saving (save with the pause menu first)", _ =>
        {
            MainThread.Run(Application.Quit);
            return AgentJson.Response(new { ok = true });
        });

        router.Post("/world/load", "Load (or create) a single-player world from the main menu: ?name=AgentWorld&mode=creative|survival&seed=&fresh=1", request =>
        {
            if (AgentGame.Player != null) throw new AgentRequestException("A world is already loaded; exit to the menu first", 409);

            var args = new List<string> { "--agent-world", AgentQuery.Text(request, "name", AgentOptions.DefaultWorld) };
            if (AgentQuery.Has(request, "mode")) args.AddRange(new[] { "--agent-mode", AgentQuery.Text(request, "mode") });
            if (AgentQuery.Has(request, "seed")) args.AddRange(new[] { "--agent-seed", AgentQuery.Text(request, "seed") });
            if (AgentQuery.Bool(request, "fresh")) args.Add("--agent-fresh");

            bool started = AgentWorldLauncher.Launch(AgentOptions.Parse(args.ToArray()));
            return AgentJson.Response(new { ok = started }, started ? 200 : 500);
        });
    }
}
