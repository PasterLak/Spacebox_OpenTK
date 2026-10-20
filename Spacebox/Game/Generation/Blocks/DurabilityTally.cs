namespace Spacebox.Game.Generation.Blocks;

public struct DurabilityTally
{
    public int Current { get; private set; }
    public int Max { get; private set; }
    public int WeakestIndex { get; private set; }
    public int WeakestMissing { get; private set; }

    public bool NeedsRepair => WeakestMissing > 0;

    public static DurabilityTally Empty => new DurabilityTally { WeakestIndex = -1 };

    public void Add(int index, byte durability, byte max)
    {
        int current = Math.Min(durability, max);
        Current += current;
        Max += max;

        int missing = max - current;
        if (WeakestIndex < 0 || missing > WeakestMissing)
        {
            WeakestIndex = index;
            WeakestMissing = missing;
        }
    }
}
