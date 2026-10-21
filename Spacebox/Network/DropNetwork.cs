using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Effects;
using Spacebox.Game.Generation;
using Spacebox.Network.Messages;

namespace Spacebox.Network;

public sealed class DropNetwork : IDropReplicator
{
    private const float MaxPickupDistance = 6f;
    private const int MaxRequestedQuantity = 10000;
    private const float MaxSpawnDistance = 24f;
    private const float MaxSpawnSpeed = 60f;

    private const int MaxDeathDropStacks = 128;

    private static readonly PeerRateLimiter _spawnRequests = new PeerRateLimiter(60, 30);
    private static readonly PeerRateLimiter _deathDropRequests = new PeerRateLimiter(1, 1.0 / 30);

    public bool IsAuthority => NetworkManager.Instance.IsServer;

    public static void Register(NetworkManager network)
    {
        network.Messages.Register<DropSpawnMessage>(OnSpawn);
        network.Messages.Register<DropSpawnRequestMessage>(OnSpawnRequest);
        network.Messages.Register<DropPickupRequestMessage>(OnPickupRequest);
        network.Messages.Register<DropPickupGrantedMessage>(OnPickupGranted);
        network.Messages.Register<DropRemovedMessage>(OnRemovedMessage);
        network.Messages.Register<DropUpdateMessage>(OnUpdateMessage);
        network.Messages.Register<DeathDropRequestMessage>(OnDeathDropRequest);
    }

    public static void SendAllTo(int peer)
    {
        var drops = World.DropEffectManager;
        if (drops == null || !NetworkManager.Instance.IsServer) return;

        foreach (var drop in drops.NetworkDrops)
            NetworkManager.Instance.Send(peer, CreateSpawnMessage(drop, drop.Velocity, drop.IsBlock, Color3Byte.White));
    }

    public void OnSpawned(Drop drop, Vector3 velocity, bool fromBlock, Color3Byte color)
    {
        NetworkManager.Instance.SendToClients(CreateSpawnMessage(drop, velocity, fromBlock, color));
    }

    public void OnQuantityChanged(Drop drop)
    {
        NetworkManager.Instance.SendToClients(new DropUpdateMessage { NetId = drop.NetId, Quantity = drop.Info.quantity, Position = drop.Position });
    }

    public void OnRemoved(Drop drop)
    {
        NetworkManager.Instance.SendToClients(new DropRemovedMessage { NetId = drop.NetId });
    }

    public void RequestSpawn(Item item, int quantity, Vector3 position, Vector3 velocity, float pickupDelay, float deceleration, SpaceEntity sourceEntity)
    {
        NetworkManager.Instance.SendToServer(new DropSpawnRequestMessage
        {
            ItemId = item.Id,
            Quantity = quantity,
            Position = position,
            Velocity = velocity,
            PickupDelay = pickupDelay,
            Deceleration = deceleration,
            SourceEntityId = sourceEntity?.EntityID ?? long.MinValue
        });
    }

    public void RequestDeathDrop(Vector3 position, IReadOnlyList<ItemStack> stacks)
    {
        var amounts = new List<ItemAmount>(stacks.Count);
        foreach (var stack in stacks) amounts.Add(new ItemAmount { ItemId = stack.Item.Id, Amount = stack.Count });
        NetworkManager.Instance.SendToServer(new DeathDropRequestMessage { Position = position, Items = amounts });
    }

    private static void OnDeathDropRequest(int sender, DeathDropRequestMessage message)
    {
        var network = NetworkManager.Instance;
        var drops = World.DropEffectManager;
        if (!network.IsServer || drops == null || sender == network.LocalPeerId) return;
        if (Spacebox.Game.Rules.ServerRules.Current.KeepInventory) return;
        if (Players.PlayerGameModeService.Of(PlayerRegistry.GetName(sender)) != Spacebox.Game.Player.GameModes.GameMode.Survival) return;
        if (message.Items == null || message.Items.Count == 0 || message.Items.Count > MaxDeathDropStacks) return;
        if (!NetValidation.IsFinite(message.Position)) return;

        var player = Combat.PlayerHitboxes.FindPlayer(sender);
        if (player == null || Vector3.DistanceSquared(player.Position, message.Position) > MaxSpawnDistance * MaxSpawnDistance) return;
        if (!_deathDropRequests.TryConsume(sender, Time.Total)) return;

        var stacks = new List<ItemStack>(message.Items.Count);
        foreach (var amount in message.Items)
        {
            if (!GameAssets.TryGetItemById(amount.ItemId, out var item)) continue;
            int limit = Math.Max(1, (int)item.StackSize) * MaxDeathDropStacks;
            if (amount.Amount <= 0 || amount.Amount > limit) continue;
            stacks.Add(new ItemStack(item, amount.Amount));
        }

        drops.DropDeathItems(message.Position, stacks);
    }

    public void RequestPickup(Drop drop)
    {
        NetworkManager.Instance.SendToServer(new DropPickupRequestMessage { NetId = drop.NetId });
    }

    private static DropSpawnMessage CreateSpawnMessage(Drop drop, Vector3 velocity, bool fromBlock, Color3Byte color)
    {
        return new DropSpawnMessage
        {
            NetId = drop.NetId,
            ItemId = drop.Info.item.Id,
            Quantity = drop.Info.quantity,
            Position = drop.Position,
            Velocity = velocity,
            PickupDelay = drop.PickupDelay,
            Deceleration = drop.Deceleration,
            SourceEntityId = drop.SourceEntity?.EntityID ?? long.MinValue,
            FromBlock = fromBlock,
            ColorR = color.R,
            ColorG = color.G,
            ColorB = color.B
        };
    }

    private static void OnSpawn(int sender, DropSpawnMessage message)
    {
        if (NetworkManager.Instance.IsServer || !GameAssets.TryGetItemById(message.ItemId, out var item)) return;

        World.DropEffectManager?.SpawnReplicated(message.NetId, item, message.Quantity, message.Position, message.Velocity,
            message.PickupDelay, message.Deceleration, FindEntity(message.SourceEntityId), message.FromBlock,
            new Color3Byte(message.ColorR, message.ColorG, message.ColorB));
    }

    private static void OnSpawnRequest(int sender, DropSpawnRequestMessage message)
    {
        if (!NetworkManager.Instance.IsServer || !GameAssets.TryGetItemById(message.ItemId, out var item)) return;
        if (!IsSpawnRequestAllowed(sender, item, message)) return;

        float speed = Math.Min(message.Velocity.Length, MaxSpawnSpeed);
        World.DropEffectManager?.DropItem(message.Position, message.Velocity, speed, item, message.Quantity,
            Math.Clamp(message.PickupDelay, 0f, 10f), Math.Clamp(message.Deceleration, 0f, 100f), FindEntity(message.SourceEntityId));
    }

    private static bool IsSpawnRequestAllowed(int sender, Item item, DropSpawnRequestMessage message)
    {
        if (message.Quantity <= 0 || message.Quantity > Math.Min(MaxRequestedQuantity, Math.Max(1, (int)item.StackSize))) return false;
        if (!NetValidation.IsFinite(message.Position) || !NetValidation.IsFinite(message.Velocity)) return false;
        if (!float.IsFinite(message.PickupDelay) || !float.IsFinite(message.Deceleration)) return false;
        if (sender == NetworkManager.Instance.LocalPeerId) return true;

        var player = Combat.PlayerHitboxes.FindPlayer(sender);
        if (player == null || Vector3.DistanceSquared(player.Position, message.Position) > MaxSpawnDistance * MaxSpawnDistance) return false;
        return _spawnRequests.TryConsume(sender, Time.Total);
    }

    private static void OnPickupRequest(int sender, DropPickupRequestMessage message)
    {
        var network = NetworkManager.Instance;
        var drops = World.DropEffectManager;
        if (!network.IsServer || drops == null) return;

        Vector3? claimer = PlayerRegistry.TryGet(sender, out var player) && player.PlayerObject?.Owner != null
            ? player.PlayerObject.Owner.Position
            : null;
        if (claimer == null && sender != network.LocalPeerId) return;

        if (!drops.TryClaim(message.NetId, claimer, MaxPickupDistance, out var item, out int quantity, out var position)) return;

        network.Send(sender, new DropPickupGrantedMessage { ItemId = item.Id, Quantity = quantity, Position = position });
    }

    private static void OnPickupGranted(int sender, DropPickupGrantedMessage message)
    {
        if (NetworkManager.Instance.IsServer || !GameAssets.TryGetItemById(message.ItemId, out var item)) return;
        World.DropEffectManager?.GrantPickup(item, message.Quantity, message.Position);
    }

    private static void OnRemovedMessage(int sender, DropRemovedMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;
        World.DropEffectManager?.RemoveReplicated(message.NetId);
    }

    private static void OnUpdateMessage(int sender, DropUpdateMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;
        World.DropEffectManager?.ApplyReplicatedQuantity(message.NetId, message.Quantity, message.Position);
    }

    private static SpaceEntity FindEntity(long entityId)
    {
        if (entityId == long.MinValue || World.Instance == null) return null;
        return World.Instance.TryFindEntity(entityId, out var entity) ? entity : null;
    }
}
