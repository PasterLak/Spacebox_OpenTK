using OpenTK.Mathematics;

namespace Spacebox.Game.Ships.Sharing;

public readonly struct ShipPlacementBounds
{
    private readonly Quaternion _inverse;

    public Vector3 Center { get; }
    public Vector3 HalfSize { get; }
    public Quaternion Rotation { get; }
    public float Radius => HalfSize.Length;
    public ShipCellSet Cells { get; }
    public Vector3 LocalCenter { get; }

    public ShipPlacementBounds(Vector3 center, Vector3 halfSize, Quaternion rotation, ShipCellSet cells = null, Vector3 localCenter = default)
    {
        Center = center;
        HalfSize = Vector3.ComponentMax(halfSize, Vector3.Zero);
        Rotation = rotation.Normalized();
        _inverse = Quaternion.Invert(Rotation);
        Cells = cells;
        LocalCenter = localCenter;
    }

    public static ShipPlacementBounds FromLocal(Vector3 localMin, Vector3 localMax, Vector3 center, Quaternion rotation, ShipCellSet cells = null)
    {
        return new ShipPlacementBounds(center, (localMax - localMin) * 0.5f, rotation, cells, (localMin + localMax) * 0.5f);
    }

    public Vector3 ToShipLocal(Vector3 worldPoint) => Vector3.Transform(worldPoint - Center, _inverse) + LocalCenter;

    public bool Overlaps(Vector3 worldPoint, float margin)
    {
        if (!Contains(worldPoint, margin)) return false;
        return Cells == null || Cells.ContainsNear(ToShipLocal(worldPoint), margin);
    }

    public bool Contains(Vector3 worldPoint, float margin)
    {
        var local = Vector3.Transform(worldPoint - Center, _inverse);
        return MathF.Abs(local.X) <= HalfSize.X + margin
            && MathF.Abs(local.Y) <= HalfSize.Y + margin
            && MathF.Abs(local.Z) <= HalfSize.Z + margin;
    }

    public Vector3 Corner(int index)
    {
        var offset = new Vector3(
            (index & 1) == 0 ? -HalfSize.X : HalfSize.X,
            (index & 2) == 0 ? -HalfSize.Y : HalfSize.Y,
            (index & 4) == 0 ? -HalfSize.Z : HalfSize.Z);
        return Center + Vector3.Transform(offset, Rotation);
    }
}
