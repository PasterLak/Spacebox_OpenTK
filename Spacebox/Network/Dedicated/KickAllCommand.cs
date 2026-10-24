using Engine;
using Engine.Commands;
using Engine.Networking;
using Spacebox.Network.Admin;

namespace Spacebox.Network.Dedicated;

public sealed class KickAllCommand : CommandBase
{
    public const string DefaultReason = "The server is going down for maintenance";

    public override string Name => "kickall";
    public override string Description => "Kick every online player except yourself, e.g. before maintenance. Usage: kickall [reason]";

    public override void Execute(string[] args)
    {
        string reason = args.Length > 0 ? string.Join(' ', args) : DefaultReason;
        var targets = Select(PlayerRegistry.All, ChatCommands.RemoteSender, NetworkManager.Instance.LocalPeerId);
        if (targets.Count == 0)
        {
            Debug.Warning("Nobody to kick");
            return;
        }

        for (int i = 0; i < targets.Count; i++) NetworkManager.Instance.Kick(targets[i].Peer, reason);
        Debug.Success($"Kicked {targets.Count} player(s): {reason}");
    }

    public static List<ConnectedPlayer> Select(IEnumerable<ConnectedPlayer> players, string exceptName, int localPeer)
    {
        var result = new List<ConnectedPlayer>();
        foreach (var player in players)
        {
            if (player.Peer == localPeer) continue;
            if (exceptName != null && string.Equals(player.Name, exceptName.Trim(), StringComparison.OrdinalIgnoreCase)) continue;
            result.Add(player);
        }
        return result;
    }
}
