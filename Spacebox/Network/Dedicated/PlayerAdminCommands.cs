using Engine;
using Engine.Commands;
using Spacebox.Game.Player.GameModes;
using Spacebox.Network.Players;

namespace Spacebox.Network.Dedicated;

public sealed class ServerGameModeCommand : CommandBase, Engine.Commands.IArgumentSuggestions
{
    public void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results)
    {
        if (argumentIndex == 0) Spacebox.Game.Commands.CommandSuggestionSources.AddGameModes(results);
        else if (argumentIndex == 1) Spacebox.Game.Commands.CommandSuggestionSources.AddPlayerNames(results);
    }

    public override string Name => "gm";
    public override string Description => "Change the game mode of a player. Usage: gm <0|1|2> <player> (survival, creative, spectator); admins in chat can leave out the player to change their own";

    public override void Execute(string[] args)
    {
        string target = args.Length >= 2 ? args[1] : Spacebox.Network.Admin.ChatCommands.CurrentSender;
        if (args.Length < 1 || target == null || !int.TryParse(args[0], out int id) || id < 0 || id > 2)
        {
            Debug.Error($"Usage: {Name} <0|1|2> <player>");
            return;
        }

        if (!PlayerAdmin.TryGetNetwork(target, out var player, out var network)) return;

        var mode = id switch { 0 => GameMode.Survival, 1 => GameMode.Creative, _ => GameMode.Spectator };
        Spacebox.Network.Players.PlayerGameModeService.Change(player.Name, mode);
        Debug.Success($"{player.Name}: game mode {mode}");
    }
}

internal static class PlayerAdmin
{
    public static bool TryGetNetwork(string nameOrPeer, out ConnectedPlayer player, out AstronautNetwork network)
    {
        network = null;
        if (!PlayerLocator.TryFind(nameOrPeer, out player))
        {
            Debug.Error($"Player not found: {nameOrPeer}");
            return false;
        }

        network = AstronautNetwork.Find(player.Peer);
        if (network != null) return true;

        Debug.Error($"{player.Name} is not spawned yet");
        return false;
    }
}
