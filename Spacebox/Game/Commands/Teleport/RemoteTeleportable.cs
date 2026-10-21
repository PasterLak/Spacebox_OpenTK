using OpenTK.Mathematics;
using Spacebox.Network;
using Spacebox.Network.Players;

namespace Spacebox.Game.Commands.Teleport;

public sealed class RemoteTeleportable : ITeleportable
{
    private readonly ConnectedPlayer _player;
    private readonly AstronautNetwork _network;

    public RemoteTeleportable(ConnectedPlayer player, AstronautNetwork network)
    {
        _player = player;
        _network = network;
    }

    public string Name => _player.Name;

    public Vector3 Position
    {
        get
        {
            PlayerLocator.TryGetPose(_player.Peer, out var position, out _);
            return position;
        }
    }

    public Vector3 Front
    {
        get
        {
            PlayerLocator.TryGetPose(_player.Peer, out _, out var front);
            return front;
        }
    }

    public void Teleport(Vector3 position) => _network.TeleportByServer(position);
}
