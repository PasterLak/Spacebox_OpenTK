using OpenTK.Mathematics;

namespace Spacebox.Game.Generation.Shapes;

public sealed class BlockShapePose
{
    public Vector3[] Positions { get; }
    public Vector3[] Normals { get; }
    public Vector3 Min { get; }
    public Vector3 Max { get; }

    public BlockShapePose(Vector3[] positions, Vector3[] normals)
    {
        Positions = positions;
        Normals = normals;

        var min = Vector3.One;
        var max = Vector3.Zero;
        for (int i = 0; i < positions.Length; i++)
        {
            min = Vector3.ComponentMin(min, positions[i]);
            max = Vector3.ComponentMax(max, positions[i]);
        }

        Min = Vector3.Clamp(min, Vector3.Zero, Vector3.One);
        Max = Vector3.Clamp(max, Vector3.Zero, Vector3.One);
    }
}
