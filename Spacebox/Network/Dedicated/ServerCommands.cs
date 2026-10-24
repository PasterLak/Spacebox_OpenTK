using Engine;
using Engine.Commands;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Network.Dedicated;

public sealed class SayCommand : CommandBase
{
    public override string Name => "say";
    public override string Description => "Send a server message to all players. Usage: say <text>";

    public override void Execute(string[] args)
    {
        if (args.Length == 0)
        {
            Debug.Error("Usage: say <text>");
            return;
        }

        ChatService.Broadcast("[Server]: " + string.Join(' ', args), Color4.Orange);
    }
}

public sealed class PlayersCommand : CommandBase
{
    private readonly string _name;

    public PlayersCommand(string name = "players")
    {
        _name = name;
    }

    public override string Name => _name;
    public override string Description => "List connected players with their ping";

    public override void Execute(string[] args)
    {
        var network = NetworkManager.Instance;
        Debug.Log($"Players online: {PlayerRegistry.All.Count}");
        foreach (var player in PlayerRegistry.All)
            Debug.Log($"  [{player.Peer}] {player.Name}, {network.GetRoundTripMilliseconds(player.Peer)} ms");
    }
}

public sealed class KickCommand : CommandBase, Engine.Commands.IArgumentSuggestions
{
    public void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results)
    {
        if (argumentIndex == 0) Spacebox.Game.Commands.CommandSuggestionSources.AddPlayerNames(results);
    }

    public override string Name => "kick";
    public override string Description => "Kick a player. Usage: kick <name|peer> [reason]";

    public override void Execute(string[] args)
    {
        if (args.Length == 0)
        {
            Debug.Error("Usage: kick <name|peer> [reason]");
            return;
        }

        ConnectedPlayer player = null;
        if (int.TryParse(args[0], out int peer)) PlayerRegistry.TryGet(peer, out player);
        if (player == null) PlayerRegistry.TryGetByName(args[0], out player);

        if (player == null)
        {
            Debug.Error($"Player not found: {args[0]}");
            return;
        }

        string reason = args.Length > 1 ? string.Join(' ', args, 1, args.Length - 1) : "Kicked by the server";
        NetworkManager.Instance.Kick(player.Peer, reason);
        Debug.Log($"Kicked {player.Name}: {reason}");
    }
}

public sealed class StatsCommand : CommandBase
{
    public override string Name => "stats";
    public override string Description => "Show server statistics: most players today and ever, joins, play time";

    public override void Execute(string[] args)
    {
        var statistics = Statistics.ServerStatisticsService.Current;
        if (statistics == null)
        {
            Debug.Log("Statistics are only kept on a dedicated server");
            return;
        }

        foreach (var line in Statistics.ServerStatisticsReport.Lines(statistics, PlayerRegistry.All.Count, DateTime.Now)) Debug.Log(line);
    }
}

public sealed class SaveCommand : CommandBase
{
    public override string Name => "save";
    public override string Description => "Save the world and all players";

    public override void Execute(string[] args)
    {
        if (NetworkSession.Current != null)
        {
            NetworkSession.Current.SaveWorld("Save");
            return;
        }

        if (World.Instance == null)
        {
            Debug.Warning("No world is loaded");
            return;
        }

        World.Instance.Save(false);
        Debug.Success("World saved");
    }
}

public sealed class StopCommand : CommandBase
{
    public override string Name => "stop";
    public override string Description => "Save and stop the server";

    public override void Execute(string[] args)
    {
        ServerRestart.Stop();
    }
}

public sealed class RestartCommand : CommandBase
{
    public override string Name => "restart";
    public override string Description => "Save and restart the server";

    public override void Execute(string[] args)
    {
        if (!ServerRestart.Restart()) Debug.Warning("The server is already stopping or restarting");
    }
}
