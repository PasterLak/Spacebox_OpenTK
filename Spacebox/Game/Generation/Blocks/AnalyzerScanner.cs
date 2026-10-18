namespace Spacebox.Game.Generation.Blocks;

public static class AnalyzerScanner
{
    private const float TicksPerSecond = 20f;

    private static readonly Random _random = new Random();
    private static readonly Dictionary<short, int> _counts = new Dictionary<short, int>();

    public static void Start(AnalyzerBlock block)
    {
        if (!block.IsActive || block.Entity == null) return;

        block.ScanComplete = false;
        block.CachedMinerals.Clear();
        block.CachedMass = 0;
        block.IsScanning = true;
        block.ScanStartAbsoluteTick = AnalyzerBlock.CurrentTick;
        block.ScanDurationTicks = GetScanTicks(block, CountBlocks(block.Entity));
        block.MarkChanged();
    }

    public static void Cancel(AnalyzerBlock block)
    {
        if (!block.IsScanning) return;

        block.IsScanning = false;
        block.ScanStartAbsoluteTick = 0;
        block.MarkChanged();
    }

    public static void Clear(AnalyzerBlock block)
    {
        if (block.IsScanning || !block.ScanComplete) return;

        block.ScanComplete = false;
        block.CachedMinerals.Clear();
        block.CachedMass = 0;
        block.MarkChanged();
    }

    public static void Update(AnalyzerBlock block)
    {
        if (!block.IsScanning || block.Entity == null) return;

        if (block.ScanDurationTicks <= 0)
        {
            block.ScanDurationTicks = GetScanTicks(block, CountBlocks(block.Entity));
            block.MarkChanged();
        }

        if (AnalyzerBlock.CurrentTick - block.ScanStartAbsoluteTick >= block.ScanDurationTicks)
            Complete(block);
    }

    public static float GetScanSeconds(AnalyzerBlock block, int blockCount)
    {
        float seconds = blockCount / 1000f * block.TimePer1000Blocks;
        return MathF.Max(seconds, block.TimePer1000Blocks);
    }

    public static int CountBlocks(SpaceEntity entity)
    {
        int count = 0;
        foreach (var chunk in entity.Chunks)
        {
            if (chunk.IsDisposed) continue;

            var blocks = chunk.Blocks;
            for (int x = 0; x < Chunk.Size; x++)
            for (int y = 0; y < Chunk.Size; y++)
            for (int z = 0; z < Chunk.Size; z++)
            {
                var block = blocks[x, y, z];
                if (block != null && block.Id != 0) count++;
            }
        }
        return count;
    }

    private static long GetScanTicks(AnalyzerBlock block, int blockCount)
    {
        return (long)(GetScanSeconds(block, blockCount) * TicksPerSecond);
    }

    private static void Complete(AnalyzerBlock block)
    {
        var entity = block.Entity!;
        block.IsScanning = false;
        block.ScanComplete = true;
        block.CachedMass = entity.Mass;
        block.CachedMinerals.Clear();

        CountMinerals(entity);

        float deviation = block.DeviationPercentage / 100f;
        foreach (var pair in _counts)
        {
            double multiplier = deviation == 0f ? 1.0 : 1.0 + (_random.NextDouble() * 2.0 - 1.0) * deviation;
            int estimated = (int)(pair.Value * multiplier);
            if (estimated > 0) block.CachedMinerals[pair.Key] = estimated;
        }

        block.MarkChanged();
    }

    private static void CountMinerals(SpaceEntity entity)
    {
        _counts.Clear();
        foreach (var chunk in entity.Chunks)
        {
            if (chunk.IsDisposed) continue;

            var blocks = chunk.Blocks;
            for (int x = 0; x < Chunk.Size; x++)
            for (int y = 0; y < Chunk.Size; y++)
            for (int z = 0; z < Chunk.Size; z++)
            {
                if (blocks[x, y, z] is not MineralBlock mineral) continue;

                _counts.TryGetValue(mineral.Id, out int current);
                _counts[mineral.Id] = current + 1;
            }
        }
    }
}
