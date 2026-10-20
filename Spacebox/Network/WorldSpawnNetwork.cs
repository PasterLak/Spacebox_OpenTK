using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Player.Respawn;

namespace Spacebox.Network;

public struct WorldSpawnMessage : INetMessage
{
    public bool HasSpawnPoint;
    public Vector3 SpawnPoint;

    public void Serialize(NetWriter writer)
    {
        writer.Write(HasSpawnPoint);
        writer.Write(SpawnPoint);
    }

    public void Deserialize(NetReader reader)
    {
        HasSpawnPoint = reader.ReadBool();
        SpawnPoint = reader.ReadVector3();
    }
}

public static class WorldSpawnNetwork
{
    public static void Register(NetworkManager network)
    {
        network.Messages.Register<WorldSpawnMessage>(OnWorldSpawn);
    }

    public static void Set(Vector3? position)
    {
        WorldSpawn.Apply(position);

        var network = NetworkManager.Instance;
        if (!network.IsServer || network.Peers.Count == 0) return;

        network.SendToClients(new WorldSpawnMessage
        {
            HasSpawnPoint = position.HasValue,
            SpawnPoint = position ?? Vector3.Zero
        }, DeliveryMode.Reliable);
    }

    private static void OnWorldSpawn(int sender, WorldSpawnMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;
        WorldSpawn.Apply(message.HasSpawnPoint ? message.SpawnPoint : null);
    }
}
