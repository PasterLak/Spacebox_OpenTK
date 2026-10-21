namespace Engine.Graphics.Transparency;

public struct TransparentEntry
{
    public ITransparentRenderable Renderable;
    public int Queue;
    public float DistanceSquared;
    public int Sequence;
}

public sealed class TransparentEntryComparer : IComparer<TransparentEntry>
{
    public static readonly TransparentEntryComparer Instance = new();

    public int Compare(TransparentEntry a, TransparentEntry b)
    {
        if (a.Queue != b.Queue) return a.Queue < b.Queue ? -1 : 1;
        if (a.DistanceSquared != b.DistanceSquared) return a.DistanceSquared > b.DistanceSquared ? -1 : 1;
        return a.Sequence.CompareTo(b.Sequence);
    }
}
