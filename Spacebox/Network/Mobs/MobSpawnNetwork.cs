using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.GUI;
using Spacebox.Game.Mobs;
using Spacebox.Game.Player;
using Spacebox.Game.Protection;
using Spacebox.Network.Combat;
using Spacebox.Network.Messages;

namespace Spacebox.Network.Mobs;

public static class MobSpawnNetwork
{
    private const float MaxSpawnDistance = 8f;
    private const float MinRequestInterval = 0.2f;
    private const float ProtectionSearchDistance = 2f;

    private static readonly Dictionary<int, float> _lastRequestTime = new Dictionary<int, float>();
    private static readonly Random _random = new Random();

    public static void Register(NetworkManager network)
    {
        network.Messages.Register<MobSpawnRequestMessage>(OnSpawnRequest);
        network.Messages.Register<MobSpawnRefundMessage>(OnRefund);
    }

    public static void Reset()
    {
        _lastRequestTime.Clear();
    }

    public static void RequestSpawn(MobSpawnerItem item, Vector3 position)
    {
        var network = NetworkManager.Instance;
        if (network.IsServer)
        {
            Spawn(item, position);
            return;
        }

        network.SendToServer(new MobSpawnRequestMessage { ItemId = item.Id, Position = position });
    }

    private static void OnSpawnRequest(int sender, MobSpawnRequestMessage message)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer || sender == network.LocalPeerId) return;
        if (!GameAssets.TryGetItemById(message.ItemId, out var item) || item is not MobSpawnerItem spawner) return;

        if (!Items.ItemAuthority.TryLose(sender, spawner.Id, 1)) return;

        bool allowed = IsRequestAllowed(sender, message.Position) && (MobSystem.Instance?.Mobs.Count ?? 0) < MaxMobsFromSpawners && !IsProtected(sender, message.Position);
        if (!allowed || !Spawn(spawner, message.Position)) Refund(sender, spawner.Id);
    }

    private const int MaxMobsFromSpawners = 96;

    private static bool IsRequestAllowed(int sender, Vector3 position)
    {
        float now = Time.Total;
        if (_lastRequestTime.TryGetValue(sender, out float last) && now - last >= 0f && now - last < MinRequestInterval) return false;

        var player = PlayerHitboxes.FindPlayer(sender);
        if (player == null || Vector3.DistanceSquared(player.Position, position) > MaxSpawnDistance * MaxSpawnDistance) return false;

        _lastRequestTime[sender] = now;
        return true;
    }

    private static bool IsProtected(int sender, Vector3 position)
    {
        if (!ProtectionNetwork.TryFindNear(position, ProtectionSearchDistance, out var entity, out _)) return false;
        if (!ProtectionNetwork.BlocksPeer(sender, entity, ProtectionRules.CellAt(entity, position))) return false;

        ProtectionNetwork.Deny(sender);
        return true;
    }

    private static void Refund(int peer, short itemId)
    {
        int seq = Items.ItemAuthority.Grant(peer, itemId, 1);
        NetworkManager.Instance.Send(peer, new MobSpawnRefundMessage { ItemId = itemId, Seq = seq });
    }

    private static void OnRefund(int sender, MobSpawnRefundMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;

        var player = PanelUI.Player;
        if (player != null && player.GameModeRules.ConsumeItems && GameAssets.TryGetItemById(message.ItemId, out var item))
            PlayerItemGiver.Give(player, item, 1);
        Items.ItemSync.Applied(message.Seq);
    }

    private static bool Spawn(MobSpawnerItem item, Vector3 position)
    {
        if (!GameAssets.Mobs.TryGetValue(item.MobId, out var definition))
        {
            Debug.Error($"[MobSpawnNetwork] Item '{item.Name}' references unknown mob '{item.MobId}'");
            return false;
        }

        return MobSystem.Instance != null && MobSystem.Instance.TrySpawn(definition, position, MobRandom.Chance(_random, definition.FleeChance));
    }
}
