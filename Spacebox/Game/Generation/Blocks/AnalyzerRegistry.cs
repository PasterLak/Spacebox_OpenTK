namespace Spacebox.Game.Generation.Blocks;

public static class AnalyzerRegistry
{
    private static readonly HashSet<AnalyzerBlock> _analyzers = new HashSet<AnalyzerBlock>();

    public static IReadOnlyCollection<AnalyzerBlock> All => _analyzers;

    public static void Add(AnalyzerBlock block)
    {
        _analyzers.Add(block);
    }

    public static void Remove(AnalyzerBlock block)
    {
        _analyzers.Remove(block);
    }

    public static void Clear()
    {
        _analyzers.Clear();
    }
}
