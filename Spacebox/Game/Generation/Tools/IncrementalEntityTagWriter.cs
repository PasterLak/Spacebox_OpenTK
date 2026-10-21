using System.Diagnostics;
using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Game.Generation.Structures;

namespace Spacebox.Game.Generation.Tools;

public sealed class IncrementalEntityTagWriter
{
    private readonly SpaceEntity _entity;
    private readonly CompoundTag _root;
    private readonly ListTag _chunks;
    private readonly Chunk[] _pending;
    private readonly HashSet<Chunk> _written = new HashSet<Chunk>();
    private int _next;

    public IncrementalEntityTagWriter(SpaceEntity entity, Vector3 sectorOrigin)
    {
        _entity = entity;
        _root = NBTHelper.BeginEntityTag(entity, sectorOrigin, out _chunks);

        var chunks = entity.Chunks;
        _pending = new Chunk[chunks.Count];
        for (int i = 0; i < _pending.Length; i++) _pending[i] = chunks[i];
    }

    public SpaceEntity Entity => _entity;
    public bool IsValid => !_entity.IsDisposed;

    public bool Step(long deadlineTimestamp)
    {
        while (_next < _pending.Length)
        {
            Write(_pending[_next++]);
            if (Stopwatch.GetTimestamp() >= deadlineTimestamp) break;
        }

        return _next >= _pending.Length;
    }

    public CompoundTag Finish()
    {
        var chunks = _entity.Chunks;
        for (int i = 0; i < chunks.Count; i++)
        {
            if (!_written.Contains(chunks[i])) Write(chunks[i]);
        }

        return NBTHelper.FinishEntityTag(_entity, _root, _chunks);
    }

    private void Write(Chunk chunk)
    {
        if (chunk == null || chunk.IsDisposed || !_written.Add(chunk)) return;
        NBTHelper.AddChunkTag(_entity, chunk, _chunks);
    }
}
