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
        if (args.Length == 0) return;
        ChatService.Broadcast("[Server]: " + string.Join(' ', args), Color4.Orange);
    }
}

public sealed class PlayersCommand : CommandBase
{
    public override string Name => "players";
    public override string Description => "List connected players";

    public override void Execute(string[] args)
    {
        Debug.Log($"Players online: {PlayerRegistry.All.Count}");
        foreach (var player in PlayerRegistry.All)
            Debug.Log($"  [{player.Peer}] {player.Name}");
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
        if (args.Length == 0) return;

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

        World.Instance?.Save(false);
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
        ServerRestart.Restart();
    }
}
