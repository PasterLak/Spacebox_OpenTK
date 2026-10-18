using Engine;
using Engine.Networking;

namespace Spacebox.Network;

public static class SpaceboxNetwork
{
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        var network = NetworkManager.Instance;
        network.ConnectionKey = "Spacebox:" + Application.Version;
        ChatService.Register(network);
        WorldEditNetwork.Register(network);
        EntitySync.Register(network);
        DropNetwork.Register(network);
        Combat.CombatNetwork.Register(network);
        Containers.ContainerNetwork.Register(network);
        Containers.StorageNameNetwork.Register(network);
        BlockStateNetwork.Register(network);
        GravityConfigNetwork.Register(network);
        DoorNetwork.Register(network);
        ExplosionNetwork.Register(network);
        GameSetPackage.Register(network);
        TimeNetwork.Register(network);
        SectorNetwork.Register(network);
        EntityCreationNetwork.Register(network);
    }
}
