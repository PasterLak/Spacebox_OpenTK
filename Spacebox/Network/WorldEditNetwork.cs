using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Network.Messages;

namespace Spacebox.Network;

public static class WorldEditNetwork
{
    public static void Register(NetworkManager network)
    {
        network.Messages.Register<BlockEditMessage>(OnBlockEdit);
    }

    public static void NotifyPlaced(SpaceEntity entity, Vector3 entitySpacePosition, Block block)
    {
        var local = entity.WorldPositionToLocal(entitySpacePosition);
        Send(new BlockEditMessage
        {
            EntityId = entity.EntityID,
            Cell = new Vector3i((int)MathF.Floor(local.X), (int)MathF.Floor(local.Y), (int)MathF.Floor(local.Z)),
            BlockId = block.Id,
            Direction = (byte)block.Direction,
            Rotation = (byte)block.Rotation
        });
    }

    public static void NotifyRemoved(Chunk chunk, Vector3Byte index)
    {
        if (chunk?.SpaceEntity == null) return;

        var origin = chunk.PositionIndex * Chunk.Size;
        Send(new BlockEditMessage
        {
            EntityId = chunk.SpaceEntity.EntityID,
            Cell = new Vector3i(origin.X + index.X, origin.Y + index.Y, origin.Z + index.Z),
            IsRemoval = true
        });
    }

    private static void Send(BlockEditMessage message)
    {
        var network = NetworkManager.Instance;
        if (!network.IsActive) return;

        if (network.IsServer)
            network.SendToClients(message);
        else
            network.SendToServer(message);
    }

    private static void OnBlockEdit(int sender, BlockEditMessage message)
    {
        var network = NetworkManager.Instance;
        if (sender == network.LocalPeerId) return;

        Apply(message);

        if (network.IsServer) network.SendToClients(message, DeliveryMode.Reliable, sender);
    }

    private static void Apply(BlockEditMessage message)
    {
        var world = World.Instance;
        if (world == null || !world.TryFindEntity(message.EntityId, out var entity)) return;

        var local = new Vector3(message.Cell.X, message.Cell.Y, message.Cell.Z);

        if (message.IsRemoval)
        {
            entity.RemoveBlockAtLocal(local, new Vector3SByte(0, 0, 0));
            return;
        }

        var block = GameAssets.CreateBlockFromId(message.BlockId);
        block.Direction = (Direction)message.Direction;
        block.Rotation = (Rotation)message.Rotation;
        entity.TryPlaceBlockLocal(local, block);
    }
}
