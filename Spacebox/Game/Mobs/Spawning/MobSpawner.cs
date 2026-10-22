using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Structures;
using Spacebox.Game.Mobs.Navigation;

namespace Spacebox.Game.Mobs.Spawning;

public sealed class MobSpawner
{
    private const int ChunkAttempts = 6;
    private const int CellAttempts = 24;
    private const float LightEpsilon = 0.01f;

    private static readonly Vector3i[] Faces =
    {
        new Vector3i(1, 0, 0), new Vector3i(-1, 0, 0),
        new Vector3i(0, 1, 0), new Vector3i(0, -1, 0),
        new Vector3i(0, 0, 1), new Vector3i(0, 0, -1)
    };

    private readonly MobSystem _system;
    private readonly Random _random = new Random();
    private readonly Dictionary<string, float> _timers = new Dictionary<string, float>();
    private readonly List<SpaceEntity> _entities = new List<SpaceEntity>();
    private readonly List<AsteroidMobRule> _entityRules = new List<AsteroidMobRule>();
    private readonly MobNavigationGrid _probe = new MobNavigationGrid();
    private MobSpawnRules _rules;
    private WorldGenerator _rulesSource;

    public MobSpawner(MobSystem system)
    {
        _system = system;
    }

    public void Update(float deltaTime)
    {
        var targets = MobTargets.All;
        if (targets.Count == 0) return;

        foreach (var definition in GameAssets.Mobs.Values)
        {
            var rules = definition.Spawn;
            if (!rules.Enabled) continue;

            _timers.TryGetValue(definition.Id, out float timer);
            timer += deltaTime;
            if (timer < rules.Interval)
            {
                _timers[definition.Id] = timer;
                continue;
            }

            _timers[definition.Id] = 0f;
            if (_system.Count(definition) >= rules.MaxTotal) continue;
            if (_random.Next(100) >= rules.Chance) continue;

            var anchor = targets[_random.Next(targets.Count)].Position;
            if (TryFindSpawnPoint(definition, anchor, out var position))
                _system.TrySpawn(definition, position, MobRandom.Chance(_random, definition.FleeChance));
        }
    }

    public bool TryFindSpawnPoint(MobDefinition definition, Vector3 anchor, out Vector3 position)
    {
        position = default;
        var rules = definition.Spawn;

        CollectEntities(definition, anchor, rules.MaxPlayerDistance);
        if (_entities.Count == 0) return false;

        for (int attempt = 0; attempt < ChunkAttempts; attempt++)
        {
            int index = _random.Next(_entities.Count);
            var entity = _entities[index];
            var rule = _entityRules[index];
            var limits = MobSpawnRules.Limits(rules, rule);
            var chunks = entity.Chunks;
            if (chunks.Count == 0) continue;
            if (rule != null && _random.Next(100) >= limits.Chance) continue;
            if (limits.MaxPerEntity > 0 && CountOnEntity(definition, entity) >= limits.MaxPerEntity) continue;

            var chunk = chunks[_random.Next(chunks.Count)];
            if (chunk.IsDisposed || CountInChunk(definition, entity, chunk) >= limits.MaxPerChunk) continue;

            _probe.Bind(entity);
            if (TryFindCell(definition, entity, rule, limits, chunk, out position)) return true;
        }

        return false;
    }

    private bool TryFindCell(MobDefinition definition, SpaceEntity entity, AsteroidMobRule rule, MobSpawnLimits limits, Chunk chunk, out Vector3 position)
    {
        var origin = ChunkOrigin(chunk);

        for (int i = 0; i < CellAttempts; i++)
        {
            var cell = origin + new Vector3i(_random.Next(Chunk.Size), _random.Next(Chunk.Size), _random.Next(Chunk.Size));
            if (!IsValidCell(definition, limits, cell)) continue;
            if (rule != null && entity is Asteroid asteroid && !MobSpawnRules.AllowsCell(rule, asteroid.NotGeneratedEntity.asteroid, new Vector3(cell.X, cell.Y, cell.Z), asteroid.NotGeneratedEntity.radiusBlocks)) continue;

            position = _probe.CellToWorld(cell);
            if (IsPlayerDistanceValid(definition.Spawn, position)) return true;
        }

        position = default;
        return false;
    }

    private bool IsValidCell(MobDefinition definition, MobSpawnLimits limits, Vector3i cell)
    {
        var rules = definition.Spawn;
        if (!_probe.TryGetBlock(cell, out _, out _, out var block) || !block.IsAir) return false;
        if (block.LightLevel > limits.MaxLightLevel + LightEpsilon) return false;
        if (!rules.RequiresNaturalBlocks) return true;

        for (int i = 0; i < Faces.Length; i++)
        {
            var neighbor = _probe.GetBlock(cell + Faces[i]);
            if (neighbor != null && !neighbor.IsAir && definition.CanEat(neighbor.Id)) return true;
        }

        return false;
    }

    private void CollectEntities(MobDefinition definition, Vector3 anchor, float radius)
    {
        _entities.Clear();
        _entityRules.Clear();

        var sector = World.SectorAt(anchor);
        if (sector?.Entities == null) return;

        bool listed = Rules().IsListed(definition.Id);
        var entities = sector.Entities;
        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            if (entity.Chunks.Count == 0) continue;
            if (DistanceToBox(entity.ToEntitySpace(anchor), entity.GeometryBoundingBox.Min, entity.GeometryBoundingBox.Max) > radius) continue;

            AsteroidMobRule rule = null;
            if (listed)
            {
                if (entity is not Asteroid { NotGeneratedEntity.asteroid: { } asteroidData } asteroid) continue;
                rule = MobSpawnRules.FindRule(asteroidData, definition.Id, asteroid.NotGeneratedEntity.biome?.IdString, entity.PositionWorld.Length);
                if (rule == null) continue;
            }

            _entities.Add(entity);
            _entityRules.Add(rule);
        }
    }

    private MobSpawnRules Rules()
    {
        var generator = World.WorldGenerator;
        if (_rules == null || _rulesSource != generator)
        {
            _rules = new MobSpawnRules(generator?.loadedAsteroids.Values);
            _rulesSource = generator;
        }
        return _rules;
    }

    private int CountOnEntity(MobDefinition definition, SpaceEntity entity)
    {
        var mobs = _system.Mobs;
        var box = entity.GeometryBoundingBox;
        int count = 0;

        for (int i = 0; i < mobs.Count; i++)
        {
            var mob = mobs[i];
            if (mob.Definition != definition || mob.IsDead) continue;
            if (DistanceToBox(entity.ToEntitySpace(mob.Position), box.Min, box.Max) <= 0f) count++;
        }

        return count;
    }

    private int CountInChunk(MobDefinition definition, SpaceEntity entity, Chunk chunk)
    {
        var mobs = _system.Mobs;
        var origin = ChunkOrigin(chunk);
        int count = 0;

        for (int i = 0; i < mobs.Count; i++)
        {
            var mob = mobs[i];
            if (mob.Definition != definition || mob.IsDead) continue;

            var local = entity.WorldToEntityLocal(mob.Position) - new Vector3(origin.X, origin.Y, origin.Z);
            if (local.X >= 0f && local.Y >= 0f && local.Z >= 0f && local.X < Chunk.Size && local.Y < Chunk.Size && local.Z < Chunk.Size)
                count++;
        }

        return count;
    }

    private static bool IsPlayerDistanceValid(MobSpawnJSON rules, Vector3 position)
    {
        var targets = MobTargets.All;
        float min = rules.MinPlayerDistance * rules.MinPlayerDistance;
        float max = rules.MaxPlayerDistance * rules.MaxPlayerDistance;
        bool anyInRange = false;

        for (int i = 0; i < targets.Count; i++)
        {
            float distance = Vector3.DistanceSquared(targets[i].Position, position);
            if (distance < min) return false;
            if (distance <= max) anyInRange = true;
        }

        return anyInRange;
    }

    private static Vector3i ChunkOrigin(Chunk chunk)
    {
        var index = chunk.PositionIndex;
        return new Vector3i(index.X * Chunk.Size, index.Y * Chunk.Size, index.Z * Chunk.Size);
    }

    private static float DistanceToBox(Vector3 point, Vector3 min, Vector3 max)
    {
        var clamped = Vector3.Clamp(point, min, max);
        return Vector3.Distance(point, clamped);
    }
}
