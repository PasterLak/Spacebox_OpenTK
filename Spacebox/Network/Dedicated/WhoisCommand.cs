using Engine;
using Engine.Commands;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Commands;
using Spacebox.Game.Player.GameModes;
using Spacebox.Network.Admin;
using Spacebox.Network.Moderation;
using Spacebox.Network.Players;

namespace Spacebox.Network.Dedicated;

public sealed class WhoisCommand : CommandBase, IArgumentSuggestions
{
    public const string Usage = "whois <name|peer>";

    public override string Name => "whois";
    public override string Description => $"Show an online player's address, ping, game mode, position, admin and mute state. Usage: {Usage}";

    public void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results)
    {
        if (argumentIndex == 0) CommandSuggestionSources.AddPlayerNames(results);
    }

    public override void Execute(string[] args)
    {
        if (args.Length == 0)
        {
            Debug.Error($"Usage: {Usage}");
            return;
        }

        string query = string.Join(' ', args);
        if (!PlayerLocator.TryFind(query, out var player))
        {
            Debug.Error($"Player not found: {query}");
            return;
        }

        foreach (var line in WhoisReport.Lines(Collect(player))) Debug.Log(line);
    }

    private static WhoisInfo Collect(ConnectedPlayer player)
    {
        var network = NetworkManager.Instance;
        Vector3? position = PlayerLocator.TryGetPose(player.Peer, out var pose, out _) ? pose : null;
        return new WhoisInfo(
            player.Name,
            player.Peer,
            network.GetPeerAddress(player.Peer),
            network.GetRoundTripMilliseconds(player.Peer),
            GameModeCatalog.NameOf(PlayerGameModeService.IdOf(player.Name)),
            position,
            AstronautNetwork.Find(player.Peer)?.IsAlive ?? false,
            ServerAdmins.IsAdmin(player.Name),
            ServerModeration.IsMuted(player.Name),
            DateTime.UtcNow - player.JoinedAtUtc);
    }
}
