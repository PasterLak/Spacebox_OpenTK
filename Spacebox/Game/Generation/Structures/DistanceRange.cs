namespace Spacebox.Game.Generation.Structures;

public readonly struct DistanceRange
{
    public static readonly DistanceRange Everywhere = new DistanceRange(0, 0);

    public DistanceRange(int min, int max)
    {
        Min = Math.Max(0, min);
        Max = max <= 0 || max == int.MaxValue ? 0 : Math.Max(Min, max);
    }

    public int Min { get; }
    public int Max { get; }
    public bool IsUnlimited => Min == 0 && Max == 0;

    public bool Contains(float distance)
    {
        if (distance < Min) return false;
        return Max == 0 || distance <= Max;
    }

    public override string ToString() => Max == 0 ? $"{Min}+" : $"{Min}-{Max}";
}
