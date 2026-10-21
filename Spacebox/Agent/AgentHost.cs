using System.Net;
using Engine;
using Engine.Web;
using Spacebox.Agent.Modules;

namespace Spacebox.Agent;

public static class AgentHost
{
    private static MiniHttpServer _server;
    private static readonly List<IAgentTicker> _tickers = new List<IAgentTicker>();

    public static AgentOptions Options { get; private set; } = new AgentOptions();
    public static AgentLog Log { get; } = new AgentLog();
    public static bool IsRunning => _server?.IsRunning == true;

    public static void Configure(string[] args)
    {
        Options = AgentOptions.Parse(args);
    }

    public static void Start()
    {
        if (!Options.Enabled || IsRunning) return;

        Log.Attach();
        Time.AlwaysCalculateFPS = true;

        var http = new HttpRouter();
        var router = new AgentRouter(http);
        foreach (var module in AgentModuleCatalog.Create(router))
        {
            module.Map(router);
            if (module is IAgentTicker ticker) _tickers.Add(ticker);
        }

        _server = new MiniHttpServer(http.Handle);
        try
        {
            _server.Start(Options.Port, IPAddress.Loopback);
            Debug.Success($"[Agent] API listening on http://127.0.0.1:{_server.Port}/");
        }
        catch (Exception ex)
        {
            Debug.Error($"[Agent] Cannot start the API on port {Options.Port}: {ex.Message}");
            _server = null;
        }
    }

    public static bool TryLoadWorld()
    {
        return Options.HasWorld && AgentWorldLauncher.Launch(Options);
    }

    public static void Update()
    {
        for (int i = 0; i < _tickers.Count; i++) _tickers[i].Tick();
    }

    public static void Stop()
    {
        _server?.Stop();
        _server = null;
        Log.Detach();
    }
}
