using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Blocks;

public sealed class BlockDamageVisuals
{
    public const float MinInterval = 0.3f;

    private readonly Dictionary<Chunk, float> _lastRemesh = new Dictionary<Chunk, float>();
    private readonly HashSet<Chunk> _dirty = new HashSet<Chunk>();
    private readonly List<Chunk> _ready = new List<Chunk>();
    private readonly Action<Chunk> _remesh;
    private readonly Func<Chunk, bool> _isAlive;
    private float _time;

    public static BlockDamageVisuals Instance { get; } = new BlockDamageVisuals(
        chunk => chunk.QueueMeshUpdate(UpdateReason.Lighting),
        chunk => !chunk.IsDisposed);

    public BlockDamageVisuals(Action<Chunk> remesh, Func<Chunk, bool> isAlive)
    {
        _remesh = remesh;
        _isAlive = isAlive;
    }

    public int PendingCount => _dirty.Count;

    public static void OnDurabilityChanged(Chunk chunk, Block block, int before)
    {
        if (chunk == null || block == null || Engine.Application.IsHeadless) return;
        int max = GameAssets.Blocks.TryGetValue(block.Id, out var data) ? data.Durability : 0;
        Instance.MarkIfStageChanged(chunk, before, block.Durability, max);
    }

    public void MarkIfStageChanged(Chunk chunk, int before, int after, int maxDurability)
    {
        if (DamageStage.Changed(before, after, maxDurability)) _dirty.Add(chunk);
    }

    public void Update(float deltaTime)
    {
        _time += deltaTime;
        if (_dirty.Count == 0) return;

        _ready.Clear();
        foreach (var chunk in _dirty)
        {
            if (!_isAlive(chunk) || !_lastRemesh.TryGetValue(chunk, out float last) || _time - last >= MinInterval)
                _ready.Add(chunk);
        }

        for (int i = 0; i < _ready.Count; i++)
        {
            var chunk = _ready[i];
            _dirty.Remove(chunk);
            if (!_isAlive(chunk))
            {
                _lastRemesh.Remove(chunk);
                continue;
            }

            _lastRemesh[chunk] = _time;
            _remesh(chunk);
        }

        if (_lastRemesh.Count > 256) ForgetOld();
    }

    public void Reset()
    {
        _dirty.Clear();
        _lastRemesh.Clear();
        _ready.Clear();
        _time = 0f;
    }

    private void ForgetOld()
    {
        _ready.Clear();
        foreach (var pair in _lastRemesh)
        {
            if (_time - pair.Value >= MinInterval) _ready.Add(pair.Key);
        }

        for (int i = 0; i < _ready.Count; i++) _lastRemesh.Remove(_ready[i]);
        _ready.Clear();
    }
}
