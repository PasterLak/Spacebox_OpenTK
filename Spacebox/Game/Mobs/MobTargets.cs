using Engine.Networking;
using Spacebox.Game.Player;
using Spacebox.Network.Players;

namespace Spacebox.Game.Mobs;

public static class MobTargets
{
    private static readonly List<MobTarget> _targets = new List<MobTarget>(8);

    public static IReadOnlyList<MobTarget> All => _targets;

    public static bool TryGet(int peer, out MobTarget target)
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            if (_targets[i].Peer != peer) continue;

            target = _targets[i];
            return true;
        }

        target = default;
        return false;
    }

    public static void Clear()
    {
        _targets.Clear();
    }

    public static void Refresh()
    {
        _targets.Clear();

        var network = NetworkManager.Instance;
        if (network == null) return;

        foreach (var networkObject in network.Spawner.Objects)
        {
            if (networkObject.Owner is not Astronaut astronaut) continue;

            var astronautNetwork = networkObject.GetBehaviour<AstronautNetwork>();
            if (astronautNetwork == null) continue;

            var position = astronautNetwork.IsOwner ? astronaut.Position : astronautNetwork.TargetPosition;
            _targets.Add(new MobTarget(networkObject.OwnerPeer, position, astronautNetwork.IsVulnerable, astronaut.Flashlight?.Enabled == true));
        }
    }
}
