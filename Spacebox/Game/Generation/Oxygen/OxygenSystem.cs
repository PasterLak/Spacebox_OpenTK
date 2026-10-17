using System.Collections.Concurrent;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;

namespace Spacebox.Game.Generation.Oxygen;

public sealed class OxygenSystem
{
    private static readonly AirFloodFill _floodFill = new AirFloodFill();

    private readonly SpaceEntity _entity;
    private readonly Dictionary<Vector3i, OxygenGeneratorBlock> _generators = new Dictionary<Vector3i, OxygenGeneratorBlock>();
    private readonly HashSet<Vector3i> _activeGenerators = new HashSet<Vector3i>();
    private readonly List<Vector3i> _generatorsToRemove = new List<Vector3i>();
    private readonly ConcurrentQueue<(Vector3i, OxygenGeneratorBlock)> _pendingGenerators = new ConcurrentQueue<(Vector3i, OxygenGeneratorBlock)>();
    private readonly AirMasks _reached = new AirMasks();
    private readonly AirMasks _oxygenated = new AirMasks();
    private bool _isDirty;

    public OxygenSystem(SpaceEntity entity)
    {
        _entity = entity;
    }

    public int GeneratorCount => _generators.Count;

    public void RegisterGenerator(Chunk chunk, int x, int y, int z, OxygenGeneratorBlock generator)
    {
        _pendingGenerators.Enqueue((ToEntity(chunk, x, y, z), generator));
    }

    public void OnBlockChanged(Chunk chunk, int x, int y, int z, Block oldBlock, Block newBlock)
    {
        var position = ToEntity(chunk, x, y, z);

        if (newBlock is OxygenGeneratorBlock generator)
        {
            _generators[position] = generator;
            _isDirty = true;
            return;
        }

        if (oldBlock is OxygenGeneratorBlock)
        {
            _generators.Remove(position);
            _isDirty = true;
            return;
        }

        if (_isDirty || _generators.Count == 0) return;

        if (TouchesReachedAir(position))
            _isDirty = true;
    }

    public void OnChunksChanged()
    {
        if (_generators.Count > 0 || !_pendingGenerators.IsEmpty)
            _isDirty = true;
    }

    public bool IsOxygenated(Vector3 localPosition)
    {
        return _oxygenated.Contains(
            (int)MathF.Floor(localPosition.X),
            (int)MathF.Floor(localPosition.Y),
            (int)MathF.Floor(localPosition.Z));
    }

    public void Update()
    {
        while (_pendingGenerators.TryDequeue(out var pending))
        {
            _generators[pending.Item1] = pending.Item2;
            _isDirty = true;
        }

        if (!_isDirty && PowerStateChanged()) _isDirty = true;
        if (!_isDirty) return;

        _isDirty = false;
        Recalculate();
    }

    private void Recalculate()
    {
        _reached.Clear();
        _oxygenated.Clear();
        _activeGenerators.Clear();

        if (_generators.Count == 0) return;

        var chunks = _entity.Chunks;
        _floodFill.BeginPass(chunks);

        foreach (var (position, generator) in _generators)
        {
            if (!_floodFill.TryGetBlock(position.X, position.Y, position.Z, out var block) || block != generator)
            {
                _generatorsToRemove.Add(position);
                continue;
            }

            if (!generator.IsActive) continue;

            _activeGenerators.Add(position);
            _floodFill.FillFrom(position.X, position.Y, position.Z, _reached, _oxygenated);
        }

        _floodFill.EndPass(chunks);

        for (int i = 0; i < _generatorsToRemove.Count; i++)
        {
            _generators.Remove(_generatorsToRemove[i]);
        }
        _generatorsToRemove.Clear();
    }

    private bool PowerStateChanged()
    {
        foreach (var (position, generator) in _generators)
        {
            if (generator.IsActive != _activeGenerators.Contains(position)) return true;
        }
        return false;
    }

    private bool TouchesReachedAir(Vector3i p)
    {
        return IsNextToGenerator(p)
               || _reached.Contains(p.X, p.Y, p.Z)
               || _reached.Contains(p.X + 1, p.Y, p.Z) || _reached.Contains(p.X - 1, p.Y, p.Z)
               || _reached.Contains(p.X, p.Y + 1, p.Z) || _reached.Contains(p.X, p.Y - 1, p.Z)
               || _reached.Contains(p.X, p.Y, p.Z + 1) || _reached.Contains(p.X, p.Y, p.Z - 1);
    }

    private bool IsNextToGenerator(Vector3i p)
    {
        return _generators.ContainsKey(new Vector3i(p.X + 1, p.Y, p.Z)) || _generators.ContainsKey(new Vector3i(p.X - 1, p.Y, p.Z))
               || _generators.ContainsKey(new Vector3i(p.X, p.Y + 1, p.Z)) || _generators.ContainsKey(new Vector3i(p.X, p.Y - 1, p.Z))
               || _generators.ContainsKey(new Vector3i(p.X, p.Y, p.Z + 1)) || _generators.ContainsKey(new Vector3i(p.X, p.Y, p.Z - 1));
    }

    private static Vector3i ToEntity(Chunk chunk, int x, int y, int z)
    {
        return new Vector3i(
            chunk.PositionIndex.X * Chunk.Size + x,
            chunk.PositionIndex.Y * Chunk.Size + y,
            chunk.PositionIndex.Z * Chunk.Size + z);
    }
}
