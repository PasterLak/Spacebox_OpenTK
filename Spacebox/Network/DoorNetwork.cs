using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Doors;
using Spacebox.Network.Containers;
using Spacebox.Network.Messages;

namespace Spacebox.Network;

public struct DoorToggleRequestMessage : INetMessage
{
    public ContainerKey Key;

    public void Serialize(NetWriter writer) => Key.Write(writer);
    public void Deserialize(NetReader reader) => Key = ContainerKey.Read(reader);
}

public struct DoorStateMessage : INetMessage
{
    public long EntityId;
    public Vector3i Home;
    public bool IsOpen;

    public void Serialize(NetWriter writer)
    {
        writer.Write(EntityId);
        writer.Write(Home);
        writer.Write(IsOpen);
    }

    public void Deserialize(NetReader reader)
    {
        EntityId = reader.ReadLong();
        Home = reader.ReadVector3i();
        IsOpen = reader.ReadBool();
    }
}

public static class DoorNetwork
{
    public static void Register(NetworkManager network)
    {
        network.Messages.Register<DoorToggleRequestMessage>(OnToggleRequest);
        network.Messages.Register<DoorStateMessage>(OnDoorState);
        DoorSystem.StateChanged += OnStateChanged;
    }

    public static void RequestToggle(Chunk chunk, Vector3Byte index)
    {
        NetworkManager.Instance.SendToServer(new DoorToggleRequestMessage { Key = ContainerResolver.KeyFor(chunk, index) });
    }

    private static void OnToggleRequest(int sender, DoorToggleRequestMessage message)
    {
        if (!NetworkManager.Instance.IsServer) return;
        if (!ContainerResolver.TryResolveBlock(message.Key, out var block, out var chunk) || block is not DoorBlock) return;
        if (!WorldEditNetwork.IsInReach(sender, chunk.SpaceEntity, message.Key.Cell)) return;
        if (ProtectionNetwork.BlocksPeerKey(sender, message.Key))
        {
            ProtectionNetwork.Deny(sender);
            return;
        }

        var origin = chunk.PositionIndex * Chunk.Size;
        var index = new Vector3Byte((byte)(message.Key.Cell.X - origin.X), (byte)(message.Key.Cell.Y - origin.Y), (byte)(message.Key.Cell.Z - origin.Z));
        chunk.SpaceEntity.Doors.Toggle(chunk, index);
    }

    private static void OnStateChanged(SpaceEntity entity, Vector3i home, bool isOpen)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer || entity == null) return;

        network.SendToClients(new DoorStateMessage { EntityId = entity.EntityID, Home = home, IsOpen = isOpen });
    }

    private static void OnDoorState(int sender, DoorStateMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;
        if (RemoteEntityBacklog.TryDefer(message.EntityId, () => OnDoorState(sender, message))) return;

        var world = World.Instance;
        if (world == null || !world.TryFindEntity(message.EntityId, out var entity)) return;

        entity.Doors.ApplyRemoteState(message.Home, message.IsOpen);
    }
}
