using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation.Tools;
using Spacebox.Network.Messages;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;

namespace Spacebox.Network;

public static class WorldEditNetwork
{
    public static void Register(NetworkManager network)
    {
        network.Messages.Register<BlockEditMessage>(OnBlockEdit);
        network.Messages.Register<BulkBlockEditMessage>(OnBulkEdit);
        network.Messages.Register<BlockDamageMessage>(OnBlockDamage);
        network.Messages.Register<BlockPlaceRefundMessage>(OnPlaceRefund);
        network.Messages.Register<BlockRepairMessage>(OnBlockRepair);
    }

    public static void NotifyRepaired(Chunk chunk, Vector3Byte index, short blockId, byte amount)
    {
        if (chunk?.SpaceEntity == null || amount == 0) return;

        var origin = chunk.PositionIndex * Chunk.Size;
        var message = new BlockRepairMessage
        {
            EntityId = chunk.SpaceEntity.EntityID,
            Cell = new Vector3i(origin.X + index.X, origin.Y + index.Y, origin.Z + index.Z),
            BlockId = blockId,
            Amount = amount
        };

        var network = NetworkManager.Instance;
        if (!network.IsActive) return;

        if (network.IsServer) network.SendToClients(message);
        else network.SendToServer(message);
    }

    private static void OnBlockRepair(int sender, BlockRepairMessage message)
    {
        var network = NetworkManager.Instance;
        if (sender == network.LocalPeerId) return;

        var world = World.Instance;
        if (world == null || !world.TryFindEntity(message.EntityId, out var entity)) return;
        if (!entity.TryGetBlockLocal(message.Cell, out var chunk, out var index)) return;

        if (network.IsServer && ProtectionNetwork.BlocksPeer(sender, entity, message.Cell)) return;
        if (network.IsServer && !IsInReach(sender, entity, message.Cell)) return;

        var block = chunk.Blocks[index.X, index.Y, index.Z];
        if (block == null || block.IsAir || block.Id != message.BlockId) return;

        byte max = GameAssets.GetBlockDataById(block.Id)?.Durability ?? block.Durability;
        byte before = block.Durability;
        block.Durability = BlockDamage.Repair(block.Durability, message.Amount, max);
        chunk.IsModified = true;
        Spacebox.Game.Generation.Blocks.BlockDamageVisuals.OnDurabilityChanged(chunk, block, before);

        if (network.IsServer) network.SendToClients(message, DeliveryMode.Reliable, sender);
    }

    private static void OnPlaceRefund(int sender, BlockPlaceRefundMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;
        PlacementRefund.Refund(PanelUI.Player, message.BlockId);
    }

    public static void NotifyDamaged(Chunk chunk, Vector3Byte index, short blockId, byte damage)
    {
        if (chunk?.SpaceEntity == null || damage == 0) return;

        var origin = chunk.PositionIndex * Chunk.Size;
        var message = new BlockDamageMessage
        {
            EntityId = chunk.SpaceEntity.EntityID,
            Cell = new Vector3i(origin.X + index.X, origin.Y + index.Y, origin.Z + index.Z),
            BlockId = blockId,
            Damage = damage
        };

        var network = NetworkManager.Instance;
        if (!network.IsActive) return;

        if (network.IsServer)
            network.SendToClients(message);
        else
            network.SendToServer(message);
    }

    private static void OnBlockDamage(int sender, BlockDamageMessage message)
    {
        var network = NetworkManager.Instance;
        if (sender == network.LocalPeerId) return;

        var world = World.Instance;
        if (world == null || !world.TryFindEntity(message.EntityId, out var entity)) return;
        if (network.IsServer && ProtectionNetwork.BlocksPeer(sender, entity, message.Cell)) return;
        if (network.IsServer && !IsInReach(sender, entity, message.Cell)) return;
        if (!entity.TryGetBlockLocal(message.Cell, out var chunk, out var index)) return;

        var block = chunk.Blocks[index.X, index.Y, index.Z];
        if (block == null || block.IsAir || block.Id != message.BlockId || Spacebox.Game.Generation.Blocks.UnbreakableBlocks.Is(block)) return;

        byte before = block.Durability;
        block.Durability = BlockDamage.Apply(block.Durability, message.Damage);
        chunk.IsModified = true;
        Spacebox.Game.Generation.Blocks.BlockDamageVisuals.OnDurabilityChanged(chunk, block, before);

        if (network.IsServer) network.SendToClients(message, DeliveryMode.Reliable, sender);
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
        NotifyPlacedAtCell(entity, new Vector3i((int)MathF.Floor(local.X), (int)MathF.Floor(local.Y), (int)MathF.Floor(local.Z)), block);
    }

    public static void NotifyPlacedAtCell(SpaceEntity entity, Vector3i cell, Block block)
    {
        Send(new BlockEditMessage
        {
            EntityId = entity.EntityID,
            Cell = cell,
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

        if (!known)
        {
            RejectUnknown(sender, message);
            return;
        }

        if (known && ProtectionNetwork.BlocksPeer(sender, entity, message.Cell))
        {
            SendCorrection(sender, entity, message.Cell);
            if (!message.IsRemoval) network.Send(sender, new BlockPlaceRefundMessage { BlockId = message.BlockId });
            ProtectionNetwork.Deny(sender);
            return;
        }

        bool isBeacon = !message.IsRemoval && Spacebox.Game.Protection.ClaimPlacement.IsBeacon(message.BlockId);
        string senderName = PlayerRegistry.GetName(sender);
        if (known && isBeacon)
        {
            string error = Spacebox.Game.Protection.ClaimRegistry.CheckPlacement(entity, message.Cell, senderName);
            if (error != null)
            {
                SendCorrection(sender, entity, message.Cell);
                network.Send(sender, new BlockPlaceRefundMessage { BlockId = message.BlockId });
                ChatService.SendToPlayer(senderName, error, OpenTK.Mathematics.Color4.Red);
                return;
            }
        }

        short previousId = known ? GetBlockId(entity, message.Cell) : (short)0;
        bool unbreakable = message.IsRemoval && Spacebox.Game.Generation.Blocks.UnbreakableBlocks.Is(previousId) &&
                           Spacebox.Network.Players.PlayerGameModeService.Of(senderName) != Spacebox.Game.Player.GameModes.GameMode.Creative;
        if (known && ((!message.IsRemoval && previousId != 0) || unbreakable || !IsInReach(sender, entity, message.Cell)))
        {
            SendCorrection(sender, entity, message.Cell);
            network.Send(sender, new BlockPlaceRefundMessage { BlockId = message.BlockId });
            return;
        }

        Apply(message);
        if (known && isBeacon) Spacebox.Game.Protection.ClaimPlacement.AssignOwner(entity, message.Cell, senderName);
        network.SendToClients(message, DeliveryMode.Reliable, sender);

        if (known && NeedsCorrection(entity, message, previousId)) SendCorrection(sender, entity, message.Cell);
        if (!message.IsRemoval && (!known || GetBlockId(entity, message.Cell) != message.BlockId))
            network.Send(sender, new BlockPlaceRefundMessage { BlockId = message.BlockId });
    }

    public const float MaxEditDistance = 16f;

    private static void RejectUnknown(int sender, BlockEditMessage message)
    {
        if (message.IsRemoval) return;
        var network = NetworkManager.Instance;
        network.Send(sender, new BlockEditMessage { EntityId = message.EntityId, Cell = message.Cell, IsRemoval = true });
        network.Send(sender, new BlockPlaceRefundMessage { BlockId = message.BlockId });
    }

    public static bool IsInReach(int peer, SpaceEntity entity, Vector3i cell)
    {
        if (peer == NetworkManager.Instance.LocalPeerId) return true;
        var player = Combat.PlayerHitboxes.FindPlayer(peer);
        if (player == null) return false;
        var world = entity.EntityLocalToWorld(new OpenTK.Mathematics.Vector3(cell.X + 0.5f, cell.Y + 0.5f, cell.Z + 0.5f));
        return OpenTK.Mathematics.Vector3.DistanceSquared(player.Position, world) <= MaxEditDistance * MaxEditDistance;
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

        if (network.IsServer && Players.PlayerGameModeService.Of(PlayerRegistry.GetName(sender)) != Spacebox.Game.Player.GameModes.GameMode.Creative)
        {
            ChatService.SendToPlayer(PlayerRegistry.GetName(sender), "Only players in creative mode can erase or fill areas", OpenTK.Mathematics.Color4.Red);
            return;
        }

        var world = World.Instance;
        if (world == null || !world.TryFindEntity(message.EntityId, out var entity)) return;
        if (network.IsServer && ProtectionNetwork.BlocksPeerBox(sender, entity, min, max))
        {
            ProtectionNetwork.Deny(sender);
            return;
        }

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
