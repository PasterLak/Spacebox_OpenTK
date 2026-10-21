using Engine;
using Engine.AI;
using Engine.Components;
using Engine.Networking;
using Spacebox.Game.Mobs.Spawning;
using Spacebox.Network;

namespace Spacebox.Game.Mobs;

public sealed class MobSystem : Component
{
    private const float DespawnCheckInterval = 2f;

    private readonly List<Mob> _mobs = new List<Mob>();
    private readonly List<Mob> _despawnBuffer = new List<Mob>();
    private readonly MobSpawner _spawner;
    private float _despawnTimer;

    public static MobSystem? Instance { get; private set; }

    public PathScheduler Paths { get; } = new PathScheduler();
    public IReadOnlyList<Mob> Mobs => _mobs;

    public MobSystem()
    {
        Instance = this;
        _spawner = new MobSpawner(this);
    }

    public void Register(Mob mob)
    {
        if (_mobs.Contains(mob)) return;

        _mobs.Add(mob);
        mob.Locomotion.Attach(Paths);
    }

    public void Unregister(Mob mob)
    {
        _mobs.Remove(mob);
    }

    public int Count(MobDefinition definition)
    {
        int count = 0;
        for (int i = 0; i < _mobs.Count; i++)
        {
            if (_mobs[i].Definition == definition && !_mobs[i].IsDead) count++;
        }
        return count;
    }

    public bool TrySpawn(MobDefinition definition, OpenTK.Mathematics.Vector3 position, bool isTimid)
    {
        var service = NetworkSession.Current?.Mobs;
        if (service == null || !NetworkManager.Instance.IsServer) return false;

        service.Spawn(definition, position, isTimid);
        return true;
    }

    public override void OnUpdate()
    {
        if (!NetworkManager.Instance.IsServer) return;

        float deltaTime = Time.Delta;
        MobTargets.Refresh();
        Paths.Update();
        _spawner.Update(deltaTime);
        UpdateDespawn(deltaTime);
    }

    public override void OnDetached()
    {
        Paths.Clear();
        _mobs.Clear();
        MobTargets.Clear();
        MobModels.Clear();
        MobSounds.Clear();
        NaturalBlocks.Reset();
        if (Instance == this) Instance = null;

        base.OnDetached();
    }

    private void UpdateDespawn(float deltaTime)
    {
        _despawnTimer -= deltaTime;
        if (_despawnTimer > 0f) return;
        _despawnTimer = DespawnCheckInterval;

        _despawnBuffer.Clear();
        for (int i = 0; i < _mobs.Count; i++)
        {
            var mob = _mobs[i];
            if (!mob.IsDead && !IsNearAnyPlayer(mob)) _despawnBuffer.Add(mob);
        }

        for (int i = 0; i < _despawnBuffer.Count; i++)
        {
            var mob = _despawnBuffer[i];
            if (mob.Link != null) mob.Link.Despawn();
            else mob.Remove();
        }
        _despawnBuffer.Clear();
    }

    private static bool IsNearAnyPlayer(Mob mob)
    {
        float distance = mob.Definition.Spawn.DespawnDistance;
        float distanceSquared = distance * distance;
        var targets = MobTargets.All;

        for (int i = 0; i < targets.Count; i++)
        {
            if (OpenTK.Mathematics.Vector3.DistanceSquared(targets[i].Position, mob.Position) <= distanceSquared) return true;
        }
        return false;
    }
}
