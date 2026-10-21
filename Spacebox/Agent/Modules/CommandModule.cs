using Engine;
using Engine.Commands;
using Engine.Web;

namespace Spacebox.Agent.Modules;

public sealed class CommandModule : IAgentModule
{
    private readonly AgentLog _log;

    public CommandModule(AgentLog log)
    {
        _log = log;
    }

    public void Map(AgentRouter router)
    {
        router.Post("/command", "Run a console command (?cmd=tp 0 0 0 or body text); returns the console lines it printed", Execute);
        router.Get("/commands", "List console commands", _ => AgentJson.Response(CommandManager.GetCommands().Select(c => new { name = c.Name, description = c.Description })));
        router.GetAsync("/log", "Console messages after ?since=<seq> (default: last ?limit=100)", request =>
        {
            long since = (long)AgentQuery.Float(request, "since", 0);
            int limit = AgentQuery.Int(request, "limit", 100);
            return Task.FromResult(AgentJson.Response(new { last = _log.LastSeq, entries = _log.Since(since, limit) }));
        });
    }

    private HttpResponse Execute(HttpRequest request)
    {
        string command = AgentQuery.TextOrBody(request, "cmd").Trim();
        long before = _log.LastSeq;
        Debug.ExecuteCommand(command);
        return AgentJson.Response(new { command, output = _log.Since(before, 200).Select(e => e.Text) });
    }
}
