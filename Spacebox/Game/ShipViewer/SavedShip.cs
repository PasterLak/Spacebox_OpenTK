using OpenTK.Mathematics;

namespace Spacebox.Game.ShipViewer;

public sealed class SavedShip
{
    public SavedShip(string filePath, string name, long entityId, ShipBlockStats stats, bool hasShipParts = true)
    {
        FilePath = filePath;
        Name = name;
        EntityId = entityId;
        Stats = stats;
        HasShipParts = hasShipParts;
    }

    public bool HasShipParts { get; }
    public bool HasCustomName => Name != EntityId.ToString();

    public string FilePath { get; }
    public string Name { get; }
    public long EntityId { get; }
    public ShipBlockStats Stats { get; }
}

public readonly struct ShipBlockStats
{
    public ShipBlockStats(int chunks, int blocks, Vector3i min, Vector3i max)
    {
        Chunks = chunks;
        Blocks = blocks;
        Min = min;
        Max = max;
    }

    public int Chunks { get; }
    public int Blocks { get; }
    public Vector3i Min { get; }
    public Vector3i Max { get; }
    public bool IsEmpty => Blocks == 0;
    public Vector3i Size => IsEmpty ? Vector3i.Zero : Max - Min;
}
