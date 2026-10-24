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
using Spacebox.Game.Player.Building;

namespace Spacebox.Network;

public static class WorldEditNetwork
{
    public static void Register(NetworkManager network)
    {
        network.Messages.Register<BlockEditMessage>(OnBlockEdit);
        network.Messages.Register<BulkBlockEditMessage>(OnBulkEdit);
        network.Messages.Register<PasteBlocksMessage>(OnPaste);
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
        if (network.IsServer && !DamageMessages.TryConsume(sender, Time.Total)) return;
        if (RemoteEntityBacklog.TryDefer(message.EntityId, () => OnBlockRepair(sender, message))) return;

        var world = World.Instance;
        if (world == null || !world.TryFindEntity(message.EntityId, out var entity)) return;
        if (!entity.TryGetBlockLocal(message.Cell, out var chunk, out var index)) return;

        if (network.IsServer && ProtectionNetwork.BlocksPeer(sender, entity, message.Cell)) return;
        if (network.IsServer && !IsInReach(sender, entity, message.Cell)) return;
        if (network.IsServer && !CanRepair(sender)) return;

        var block = chunk.Blocks[index.X, index.Y, index.Z];
        if (block == null || block.IsAir || block.Id != message.BlockId) return;

        if (network.IsServer) message.Amount = ContentBlockEditLimits.Current.ClampRepair(message.Amount);
        if (message.Amount == 0) return;

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
        Items.ItemSync.Applied(message.Seq);
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
        if (RemoteEntityBacklog.TryDefer(message.EntityId, () => OnBlockDamage(sender, message))) return;

        if (network.IsServer && !DamageMessages.TryConsume(sender, Time.Total)) return;

        var world = World.Instance;
        if (world == null || !world.TryFindEntity(message.EntityId, out var entity)) return;
        if (network.IsServer && ProtectionNetwork.BlocksPeer(sender, entity, message.Cell)) return;
        if (network.IsServer && !IsInReach(sender, entity, message.Cell)) return;
        if (network.IsServer && !CanDamage(sender)) return;
        if (!entity.TryGetBlockLocal(message.Cell, out var chunk, out var index)) return;

        var block = chunk.Blocks[index.X, index.Y, index.Z];
        if (block == null || block.IsAir || block.Id != message.BlockId || Spacebox.Game.Generation.Blocks.UnbreakableBlocks.Is(block)) return;

        if (network.IsServer)
        {
            byte blockMax = GameAssets.GetBlockDataById(block.Id)?.Durability ?? block.Durability;
            message.Damage = ContentBlockEditLimits.Current.ClampDamage(message.Damage, block.IsTransparent, blockMax);
        }
        if (message.Damage == 0) return;

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

        var network = NetworkManager.Instance;
        var message = new BulkBlockEditMessage { EntityId = entity.EntityID, Min = min, Max = max, BlockId = blockId };
        if (network.IsRemoteClient)
        {
            network.SendToServer(message);
            return true;
        }

        BulkBlockEdit.Apply(entity, min, max, blockId);
        if (network.IsActive) network.SendToClients(message);
        return true;
    }

    public static bool RequestPaste(SpaceEntity entity, Vector3i min, BlockClipboard clipboard)
    {
        if (entity == null || clipboard == null) return false;

        var cells = new List<ClipboardCell>(clipboard.Cells);
        if (!BlockPaste.TryValidate(min, clipboard.Size, cells, out var error))
        {
            Debug.Warning($"[WorldEdit] {error}");
            return false;
        }

        var network = NetworkManager.Instance;
        var message = new PasteBlocksMessage { EntityId = entity.EntityID, Min = min, Size = clipboard.Size, Cells = cells };
        if (network.IsRemoteClient)
        {
            network.SendToServer(message);
            return true;
        }

        BlockPaste.Apply(entity, min, cells);
        if (network.IsActive) network.SendToClients(message);
        return true;
    }

    private static void OnPaste(int sender, PasteBlocksMessage message)
    {
        var network = NetworkManager.Instance;
        if (sender == network.LocalPeerId) return;
        if (RemoteEntityBacklog.TryDefer(message.EntityId, () => OnPaste(sender, message))) return;

        if (!BlockPaste.TryValidate(message.Min, message.Size, message.Cells, out var error))
        {
            Debug.Warning($"[WorldEdit] Rejected paste from peer {sender}: {error}");
            return;
        }

        if (network.IsServer && !Players.PlayerGameModeService.RulesOf(PlayerRegistry.GetName(sender)).AreaTools)
        {
            ChatService.SendToPlayer(PlayerRegistry.GetName(sender), "Your game mode cannot paste blocks", OpenTK.Mathematics.Color4.Red);
            return;
        }

        var world = World.Instance;
        if (world == null || !world.TryFindEntity(message.EntityId, out var entity)) return;
        var max = message.Min + message.Size - Vector3i.One;
        if (network.IsServer && ProtectionNetwork.BlocksPeerBox(sender, entity, message.Min, max))
        {
            ProtectionNetwork.Deny(sender);
            return;
        }

        BlockPaste.Apply(entity, message.Min, message.Cells);
        if (network.IsServer) network.SendToClients(message);
    }

    private static bool CanDamage(int sender)
    {
        return sender == NetworkManager.Instance.LocalPeerId || Players.PlayerGameModeService.RulesOf(PlayerRegistry.GetName(sender)).BreakBlocks;
    }

    private static bool CanRepair(int sender)
    {
        if (sender == NetworkManager.Instance.LocalPeerId) return true;
        var rules = Players.PlayerGameModeService.RulesOf(PlayerRegistry.GetName(sender));
        return rules.PlaceBlocks && rules.UseItems;
    }

    private const float EditSoundMinDistance = 2f;
    private const float EditSoundMaxDistance = 10f;
    private const float EditSoundRolloff = 1.5f;

    private static readonly SpatialSoundPool EditSounds = new SpatialSoundPool(4, EditSoundMinDistance, EditSoundMaxDistance, EditSoundRolloff);
    private static readonly Random SoundRandom = new Random();

    private static readonly PendingEntityEdits<BlockEditMessage> PendingEdits = new PendingEntityEdits<BlockEditMessage>();
    private static readonly PeerRateLimiter DamageMessages = new PeerRateLimiter(200, 100);
    private static readonly PeerRateLimiter Removals = new PeerRateLimiter(120, 60);

    public static void Reset()
    {
        EditSounds.Clear();
        PendingEdits.Clear();
        DamageMessages.Clear();
        Removals.Clear();
        ContentBlockEditLimits.Reset();
    }

    public static void ForgetPeer(int peer)
    {
        DamageMessages.Forget(peer);
        Removals.Forget(peer);
        foreach (var message in PendingEdits.RemovePeer(peer))
        {
            if (!message.IsRemoval && GameAssets.TryGetItemByBlockID(message.BlockId, out var item))
                Items.ItemAuthority.Gain(peer, item.Id, 1);
        }
    }

    public static void Update()
    {
        if (!NetworkManager.Instance.IsServer) return;
        PendingEdits.Update(Time.Delta, IsEntityLoaded, ProcessBlockEdit, ExpirePending);
    }

    private static bool IsEntityLoaded(long entityId)
    {
        return World.Instance != null && World.Instance.TryFindEntity(entityId, out _);
    }

    private static void ExpirePending(int sender, BlockEditMessage message)
    {
        Debug.Warning($"[WorldEdit] Entity {message.EntityId} did not load in time, an edit from peer {sender} was dropped");
        RejectUnknown(sender, message);
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
            if (RemoteEntityBacklog.TryDefer(message.EntityId, () => OnBlockEdit(sender, message))) return;
            Apply(message);
            return;
        }

        if (!message.IsRemoval && !ChargePlacement(sender, message.BlockId))
        {
            network.Send(sender, new BlockEditMessage { EntityId = message.EntityId, Cell = message.Cell, IsRemoval = true });
            return;
        }

        ProcessBlockEdit(sender, message);
    }

    internal static bool ChargePlacement(int sender, short blockId)
    {
        return !GameAssets.TryGetItemByBlockID(blockId, out var item) || Items.ItemAuthority.TryLose(sender, item.Id, 1);
    }

    internal static void SendRefund(int peer, short blockId)
    {
        int seq = GameAssets.TryGetItemByBlockID(blockId, out var item) ? Items.ItemAuthority.Grant(peer, item.Id, 1) : 0;
        NetworkManager.Instance.Send(peer, new BlockPlaceRefundMessage { BlockId = blockId, Seq = seq });
    }

    private static void ProcessBlockEdit(int sender, BlockEditMessage message)
    {
        var network = NetworkManager.Instance;
        var world = World.Instance;
        SpaceEntity entity = null;
        bool known = world != null && world.TryFindEntity(message.EntityId, out entity);

        if (!known || PendingEdits.Has(message.EntityId))
        {
            if (!PendingEdits.TryAdd(sender, message.EntityId, message)) RejectUnknown(sender, message);
            return;
        }

        if (known && ProtectionNetwork.BlocksPeer(sender, entity, message.Cell))
        {
            SendRejection(sender, entity, message);
            if (!message.IsRemoval) SendRefund(sender, message.BlockId);
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
                SendRejection(sender, entity, message);
                SendRefund(sender, message.BlockId);
                ChatService.SendToPlayer(senderName, error, OpenTK.Mathematics.Color4.Red);
                return;
            }
        }

        short previousId = known ? GetBlockId(entity, message.Cell) : (short)0;
        bool unbreakable = message.IsRemoval && Spacebox.Game.Generation.Blocks.UnbreakableBlocks.Is(previousId) &&
                           !Spacebox.Network.Players.PlayerGameModeService.RulesOf(senderName).BreakUnbreakable;
        bool unbroken = message.IsRemoval && known && !CanRemoveNow(sender, senderName, entity, message.Cell);
        bool placeDenied = !message.IsRemoval && sender != network.LocalPeerId && !Spacebox.Network.Players.PlayerGameModeService.RulesOf(senderName).PlaceBlocks;
        if (known && ((!message.IsRemoval && previousId != 0) || unbreakable || unbroken || placeDenied || !IsInReach(sender, entity, message.Cell) || IsBlockedByBody(entity, message)))
        {
            SendRejection(sender, entity, message);
            if (!message.IsRemoval) SendRefund(sender, message.BlockId);
            return;
        }

        if (Apply(message))
        {
            if (known && isBeacon) Spacebox.Game.Protection.ClaimPlacement.AssignOwner(entity, message.Cell, senderName);
            network.SendToClients(message, DeliveryMode.Reliable, sender);
        }

        if (known && NeedsCorrection(entity, message, previousId)) SendRejection(sender, entity, message);
        if (!message.IsRemoval && (!known || GetBlockId(entity, message.Cell) != message.BlockId))
            SendRefund(sender, message.BlockId);
    }

    private static bool IsBlockedByBody(SpaceEntity entity, BlockEditMessage message)
    {
        if (message.IsRemoval) return false;

        var block = GameAssets.CreateBlockFromId(message.BlockId);
        block.Direction = (Direction)message.Direction;
        block.Rotation = (Rotation)message.Rotation;
        return entity.IsBlockAreaOccupiedByBodyAtCell(message.Cell, block);
    }

    public const float MaxEditDistance = 16f;

    private static bool CanRemoveNow(int sender, string senderName, SpaceEntity entity, Vector3i cell)
    {
        if (sender == NetworkManager.Instance.LocalPeerId) return true;
        if (!Spacebox.Network.Players.PlayerGameModeService.RulesOf(senderName).BreakBlocks) return false;
        if (Spacebox.Network.Players.PlayerGameModeService.RulesOf(senderName).InstantBreak) return true;
        if (!entity.TryGetBlockLocal(cell, out var chunk, out var index)) return true;

        var block = chunk.Blocks[index.X, index.Y, index.Z];
        if (block == null || block.IsAir) return true;
        return block.Durability == 0 && Removals.TryConsume(sender, Time.Total);
    }

    private static void RejectUnknown(int sender, BlockEditMessage message)
    {
        if (message.IsRemoval) return;
        var network = NetworkManager.Instance;
        network.Send(sender, new BlockEditMessage { EntityId = message.EntityId, Cell = message.Cell, IsRemoval = true });
        SendRefund(sender, message.BlockId);
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

    private static readonly List<MultiblockCell> RejectedFootprint = new List<MultiblockCell>();
    private static readonly HashSet<Vector3i> CorrectedCells = new HashSet<Vector3i>();

    private static void SendRejection(int peer, SpaceEntity entity, BlockEditMessage message)
    {
        if (message.IsRemoval || !GameAssets.Blocks.TryGetValue(message.BlockId, out var data) || !data.IsMultiblock)
        {
            SendCorrection(peer, entity, message.Cell);
            return;
        }

        var network = NetworkManager.Instance;
        network.Send(peer, new BlockEditMessage { EntityId = entity.EntityID, Cell = message.Cell, IsRemoval = true });

        MultiblockLayout.GetCells(data, (Direction)message.Direction, (Rotation)message.Rotation, RejectedFootprint);
        CorrectedCells.Clear();
        for (int i = 0; i < RejectedFootprint.Count; i++)
        {
            if (TryBuildCorrection(entity, message.Cell + RejectedFootprint[i].Offset, out var correction) && !correction.IsRemoval && CorrectedCells.Add(correction.Cell))
                network.Send(peer, correction);
        }
    }

    private static void SendCorrection(int peer, SpaceEntity entity, Vector3i cell)
    {
        if (TryBuildCorrection(entity, cell, out var correction)) NetworkManager.Instance.Send(peer, correction);
    }

    private static bool TryBuildCorrection(SpaceEntity entity, Vector3i cell, out BlockEditMessage correction)
    {
        correction = new BlockEditMessage { EntityId = entity.EntityID, Cell = cell, IsRemoval = true };
        if (!entity.TryGetBlockLocal(cell, out var chunk, out var index)) return true;

        var block = chunk.Blocks[index.X, index.Y, index.Z];
        if (block == null || block.IsAir) return true;

        if (block.HasMultiblockOffset)
        {
            if (!entity.TryGetStructureOrigin(chunk, index, out var originChunk, out var originIndex)) return false;
            block = originChunk.Blocks[originIndex.X, originIndex.Y, originIndex.Z];
            correction.Cell = Spacebox.Game.Protection.ProtectionRules.CellOf(originChunk, originIndex);
        }

        correction.IsRemoval = false;
        correction.BlockId = block.Id;
        correction.Direction = (byte)block.Direction;
        correction.Rotation = (byte)block.Rotation;
        return true;
    }

    private static void OnBulkEdit(int sender, BulkBlockEditMessage message)
    {
        var network = NetworkManager.Instance;
        if (sender == network.LocalPeerId) return;
        if (RemoteEntityBacklog.TryDefer(message.EntityId, () => OnBulkEdit(sender, message))) return;

        var min = message.Min;
        var max = message.Max;
        BulkBlockEdit.Normalize(ref min, ref max);
        if (!BulkBlockEdit.TryValidate(min, max, message.BlockId, out var error))
        {
            Debug.Warning($"[WorldEdit] Rejected bulk edit from peer {sender}: {error}");
            return;
        }

        if (network.IsServer && !Players.PlayerGameModeService.RulesOf(PlayerRegistry.GetName(sender)).AreaTools)
        {
            ChatService.SendToPlayer(PlayerRegistry.GetName(sender), "Your game mode cannot erase or fill areas", OpenTK.Mathematics.Color4.Red);
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
        if (network.IsServer) network.SendToClients(message);
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

    private static bool Apply(BlockEditMessage message)
    {
        var world = World.Instance;
        if (world == null || !world.TryFindEntity(message.EntityId, out var entity)) return false;

        var local = new Vector3(message.Cell.X, message.Cell.Y, message.Cell.Z);

        if (message.IsRemoval)
        {
            short removedId = GetBlockId(entity, message.Cell);
            if (NetworkManager.Instance.IsServer) InvokeDestroy(entity, message.Cell);
            entity.RemoveBlockAtLocal(local, new Vector3SByte(0, 0, 0));
            if (removedId > 0) PlayEditSound(entity, message.Cell, removedId, BlockInteractionType.Destroy);
            return true;
        }

        var block = GameAssets.CreateBlockFromId(message.BlockId);
        block.Direction = (Direction)message.Direction;
        block.Rotation = (Rotation)message.Rotation;
        if (!entity.TryPlaceBlockLocal(local, block)) return false;

        PlayEditSound(entity, message.Cell, message.BlockId, BlockInteractionType.Place);
        return true;
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
