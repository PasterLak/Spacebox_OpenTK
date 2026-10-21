using System.Globalization;
using Engine;
using Engine.Commands;
using OpenTK.Mathematics;
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

public sealed class ServerTeleportCommand : CommandBase, Engine.Commands.IArgumentSuggestions
{
    public void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results)
    {
        if (argumentIndex == 0) Spacebox.Game.Commands.CommandSuggestionSources.AddPlayerNames(results);
    }

    public override string Name => "tp";
    public override string Description => "Teleport a player. Usage: tp <player> <x> <y> <z>";

    public override void Execute(string[] args)
    {
        if (args.Length < 4 || !TryParseFloat(args[1], out float x) || !TryParseFloat(args[2], out float y) || !TryParseFloat(args[3], out float z))
        {
            Debug.Error($"Usage: {Name} <player> <x> <y> <z>");
            return;
        }

        if (!PlayerAdmin.TryGetNetwork(args[0], out var player, out var network)) return;

        network.TeleportByServer(new Vector3(x, y, z));
        Debug.Success($"{player.Name} teleported to ({x}, {y}, {z})");
    }

    private static bool TryParseFloat(string value, out float result)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }
}

public sealed class ServerTeleportToPlayerCommand : CommandBase, Engine.Commands.IArgumentSuggestions
{
    public void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results)
    {
        if (argumentIndex < 2) Spacebox.Game.Commands.CommandSuggestionSources.AddPlayerNames(results);
    }

    private const float Offset = 2f;

    public override string Name => "tpp";
    public override string Description => "Teleport a player to another player. Usage: tpp <player> <target>";

    public override void Execute(string[] args)
    {
        if (args.Length < 2)
        {
            Debug.Error($"Usage: {Name} <player> <target>");
            return;
        }

        if (!PlayerAdmin.TryGetNetwork(args[0], out var player, out var network)) return;
        if (!PlayerLocator.TryFind(args[1], out var target) || !PlayerLocator.TryGetPose(target.Peer, out var position, out var front))
        {
            Debug.Error($"Player not found: {args[1]}");
            return;
        }

        network.TeleportByServer(position + front * Offset);
        Debug.Success($"{player.Name} teleported to {target.Name}");
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
