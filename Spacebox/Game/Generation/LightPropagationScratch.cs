using Engine;

namespace Spacebox.Game.Generation;

public sealed class LightPropagationScratch
{
    public static readonly ConcurrentPool<LightPropagationScratch> Pool = new ConcurrentPool<LightPropagationScratch>(
        () => new LightPropagationScratch(),
        scratch => scratch.Clear(),
        Math.Max(4, Environment.ProcessorCount));

    public readonly Chunk[] RegionChunks = new Chunk[27];
    public readonly Dictionary<Chunk, int> ChunkToIndex = new Dictionary<Chunk, int>();
    public readonly List<Chunk> IndexToChunk = new List<Chunk>();
    public readonly Queue<uint> LightQueue = new Queue<uint>(2048);
    public readonly HashSet<Chunk> ChangedChunks = new HashSet<Chunk>();

    public void Clear()
    {
        Array.Clear(RegionChunks);
        ChunkToIndex.Clear();
        IndexToChunk.Clear();
        LightQueue.Clear();
        ChangedChunks.Clear();
    }
}
