using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;
using Spacebox.Network.Messages;

namespace Spacebox.Network;

public static class WorldEditNetwork
{
    public static void Register(NetworkManager network)
    {
        network.Messages.Register<BlockEditMessage>(OnBlockEdit);
        network.Messages.Register<BulkBlockEditMessage>(OnBulkEdit);
    }

    public static bool RequestBulkEdit(SpaceEntity entity, Vector3i min, Vector3i max, short blockId)
    {
        if (entity == null) return false;

        BulkBlockEdit.Normalize(ref min, ref max);
        if (!BulkBlockEdit.TryValidate(min, max, blockId, out var error))
        {
            Debug.Warning($"[WorldEdit] {error}");
            return false;
        }

        BulkBlockEdit.Apply(entity, min, max, blockId);

        var network = NetworkManager.Instance;
        if (!network.IsActive) return true;

        var message = new BulkBlockEditMessage { EntityId = entity.EntityID, Min = min, Max = max, BlockId = blockId };
        if (network.IsServer) network.SendToClients(message);
        else network.SendToServer(message);
        return true;
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

    private static void OnBulkEdit(int sender, BulkBlockEditMessage message)
    {
        var network = NetworkManager.Instance;
        if (sender == network.LocalPeerId) return;

        var min = message.Min;
        var max = message.Max;
        BulkBlockEdit.Normalize(ref min, ref max);
        if (!BulkBlockEdit.TryValidate(min, max, message.BlockId, out var error))
        {
            Debug.Warning($"[WorldEdit] Rejected bulk edit from peer {sender}: {error}");
            return;
        }

        var world = World.Instance;
        if (world == null || !world.TryFindEntity(message.EntityId, out var entity)) return;

        BulkBlockEdit.Apply(entity, min, max, message.BlockId);

        message.Min = min;
        message.Max = max;
        if (network.IsServer) network.SendToClients(message, DeliveryMode.Reliable, sender);
    }

    private static void InvokeDestroy(SpaceEntity entity, Vector3i cell)
    {
        if (!entity.TryGetBlockLocal(cell, out var chunk, out var index)) return;

        var block = chunk.Blocks[index.X, index.Y, index.Z];
        if (block == null || block.IsAir || block.OnDestroy == null) return;

        block.OnDestroy.Invoke(new Spacebox.Game.Physics.HitInfo
        {
            chunk = chunk,
            block = block,
            blockPositionIndex = index,
            blockPositionEntity = cell,
            blockPositionWorld = entity.EntityLocalToWorld(new Vector3(cell.X + 0.5f, cell.Y + 0.5f, cell.Z + 0.5f))
        });
    }

    private static void Apply(BlockEditMessage message)
    {
        var world = World.Instance;
        if (world == null || !world.TryFindEntity(message.EntityId, out var entity)) return;

        var local = new Vector3(message.Cell.X, message.Cell.Y, message.Cell.Z);

        if (message.IsRemoval)
        {
            if (NetworkManager.Instance.IsServer) InvokeDestroy(entity, message.Cell);
            entity.RemoveBlockAtLocal(local, new Vector3SByte(0, 0, 0));
            return;
        }

        var block = GameAssets.CreateBlockFromId(message.BlockId);
        block.Direction = (Direction)message.Direction;
        block.Rotation = (Rotation)message.Rotation;
        entity.TryPlaceBlockLocal(local, block);
    }
}
