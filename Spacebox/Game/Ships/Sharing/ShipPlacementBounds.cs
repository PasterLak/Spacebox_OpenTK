using OpenTK.Mathematics;

namespace Spacebox.Game.Ships.Sharing;

public readonly struct ShipPlacementBounds
{
    private readonly Quaternion _inverse;

    public Vector3 Center { get; }
    public Vector3 HalfSize { get; }
    public Quaternion Rotation { get; }
    public float Radius => HalfSize.Length;

    public ShipPlacementBounds(Vector3 center, Vector3 halfSize, Quaternion rotation)
    {
        Center = center;
        HalfSize = Vector3.ComponentMax(halfSize, Vector3.Zero);
        Rotation = rotation.Normalized();
        _inverse = Quaternion.Invert(Rotation);
    }

    public static ShipPlacementBounds FromLocal(Vector3 localMin, Vector3 localMax, Vector3 center, Quaternion rotation)
    {
        return new ShipPlacementBounds(center, (localMax - localMin) * 0.5f, rotation);
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
