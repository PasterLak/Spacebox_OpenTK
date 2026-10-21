using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Mobs;
using Spacebox.Network.Combat;
using Spacebox.Network.Messages;

namespace Spacebox.Network.Mobs;

public static class MobSpawnNetwork
{
    private const float MaxSpawnDistance = 8f;
    private const float MinRequestInterval = 0.2f;

    private static readonly Dictionary<int, float> _lastRequestTime = new Dictionary<int, float>();
    private static readonly Random _random = new Random();

    public static void Register(NetworkManager network)
    {
        network.Messages.Register<MobSpawnRequestMessage>(OnSpawnRequest);
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
        if (!IsRequestAllowed(sender, message.Position)) return;
        if ((Spacebox.Game.Mobs.MobSystem.Instance?.Mobs.Count ?? 0) >= MaxMobsFromSpawners) return;

        Spawn(spawner, message.Position);
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

    private static void Spawn(MobSpawnerItem item, Vector3 position)
    {
        if (!GameAssets.Mobs.TryGetValue(item.MobId, out var definition))
        {
            Debug.Error($"[MobSpawnNetwork] Item '{item.Name}' references unknown mob '{item.MobId}'");
            return;
        }

        MobSystem.Instance?.TrySpawn(definition, position, MobRandom.Chance(_random, definition.FleeChance));
    }
}
