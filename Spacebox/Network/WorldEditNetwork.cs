using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Effects;
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

    private const float EditSoundMinDistance = 2f;
    private const float EditSoundMaxDistance = 10f;
    private const float EditSoundRolloff = 1.5f;

    private static readonly SpatialSoundPool EditSounds = new SpatialSoundPool(4, EditSoundMinDistance, EditSoundMaxDistance, EditSoundRolloff);
    private static readonly Random SoundRandom = new Random();

    public static void Reset()
    {
        EditSounds.Clear();
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

        if (!network.IsServer)
        {
            Apply(message);
            return;
        }

        var world = World.Instance;
        SpaceEntity entity = null;
        bool known = world != null && world.TryFindEntity(message.EntityId, out entity);
        short previousId = known ? GetBlockId(entity, message.Cell) : (short)0;

        Apply(message);
        network.SendToClients(message, DeliveryMode.Reliable, sender);

        if (known && NeedsCorrection(entity, message, previousId)) SendCorrection(sender, entity, message.Cell);
    }

    private static bool NeedsCorrection(SpaceEntity entity, BlockEditMessage message, short previousId)
    {
        short currentId = GetBlockId(entity, message.Cell);
        if (message.IsRemoval) return currentId != 0;
        return currentId != message.BlockId || previousId != 0;
    }

    private static void SendCorrection(int peer, SpaceEntity entity, Vector3i cell)
    {
        var correction = new BlockEditMessage { EntityId = entity.EntityID, Cell = cell, IsRemoval = true };

        if (entity.TryGetBlockLocal(cell, out var chunk, out var index))
        {
            var block = chunk.Blocks[index.X, index.Y, index.Z];
            if (block != null && !block.IsAir)
            {
                if (block.HasMultiblockOffset) return;

                correction.IsRemoval = false;
                correction.BlockId = block.Id;
                correction.Direction = (byte)block.Direction;
                correction.Rotation = (byte)block.Rotation;
            }
        }

        NetworkManager.Instance.Send(peer, correction);
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
            short removedId = GetBlockId(entity, message.Cell);
            if (NetworkManager.Instance.IsServer) InvokeDestroy(entity, message.Cell);
            entity.RemoveBlockAtLocal(local, new Vector3SByte(0, 0, 0));
            if (removedId > 0) PlayEditSound(entity, message.Cell, removedId, BlockInteractionType.Destroy);
            return;
        }

        var block = GameAssets.CreateBlockFromId(message.BlockId);
        block.Direction = (Direction)message.Direction;
        block.Rotation = (Rotation)message.Rotation;
        if (entity.TryPlaceBlockLocal(local, block)) PlayEditSound(entity, message.Cell, message.BlockId, BlockInteractionType.Place);
    }

    private static short GetBlockId(SpaceEntity entity, Vector3i cell)
    {
        if (!entity.TryGetBlockLocal(cell, out var chunk, out var index)) return 0;

        var block = chunk.Blocks[index.X, index.Y, index.Z];
        return block == null || block.IsAir ? (short)0 : block.Id;
    }

    private static void PlayEditSound(SpaceEntity entity, Vector3i cell, short blockId, BlockInteractionType type)
    {
        if (Application.IsHeadless) return;

        var listener = Camera.Main;
        var position = entity.EntityLocalToWorld(new Vector3(cell.X + 0.5f, cell.Y + 0.5f, cell.Z + 0.5f));
        if (listener == null || Vector3.DistanceSquared(listener.Position, position) > EditSoundMaxDistance * EditSoundMaxDistance) return;

        EditSounds.Play(BlockSounds.GetClip(blockId, type), position, SoundRandom.Next(9, 12) * 0.1f);
    }
}
