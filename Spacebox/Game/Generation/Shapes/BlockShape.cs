using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Tools;

namespace Spacebox.Game.Generation.Shapes;

public sealed class BlockShape
{
    private const int DirectionCount = 6;
    private const int RotationCount = 4;
    private static readonly Vector3 CellCenter = new Vector3(0.5f);

    private readonly BlockShapePose[] _poses = new BlockShapePose[DirectionCount * RotationCount * 2];
    private readonly Vector3SByte[] _supportOffsets = new Vector3SByte[DirectionCount * RotationCount];

    public BlockGeometry Geometry { get; }
    public Face[] VertexFaces { get; }

    public BlockShape(BlockGeometry geometry)
    {
        Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
        VertexFaces = new Face[geometry.VertexCount];
        for (int i = 0; i < VertexFaces.Length; i++)
            VertexFaces[i] = DominantFace(geometry.Normals[i]);

        var bottomCenter = new Vector3((geometry.Min.X + geometry.Max.X) * 0.5f, geometry.Min.Y, (geometry.Min.Z + geometry.Max.Z) * 0.5f);
        var supportedOffset = new Vector3(0f, -0.5f, 0f) - bottomCenter;
        var freeOffset = -(geometry.Min + geometry.Max) * 0.5f;

        for (int d = 0; d < DirectionCount; d++)
        {
            for (int r = 0; r < RotationCount; r++)
            {
                var transform = BlockRotationHelper.CalculateTransformMatrix(Direction.Up, (Direction)d, (Rotation)r);
                int index = d * RotationCount + r;

                var down = Vector3.TransformVector(-Vector3.UnitY, transform);
                _supportOffsets[index] = new Vector3SByte((sbyte)MathF.Round(down.X), (sbyte)MathF.Round(down.Y), (sbyte)MathF.Round(down.Z));

                _poses[index * 2] = CreatePose(transform, freeOffset);
                _poses[index * 2 + 1] = CreatePose(transform, supportedOffset);
            }
        }
    }

    public BlockShapePose GetPose(Direction direction, Rotation rotation, bool supported)
    {
        return _poses[((int)direction * RotationCount + (int)rotation) * 2 + (supported ? 1 : 0)];
    }

    public Vector3SByte GetSupportOffset(Direction direction, Rotation rotation)
    {
        return _supportOffsets[(int)direction * RotationCount + (int)rotation];
    }

    private BlockShapePose CreatePose(Matrix4 transform, Vector3 offset)
    {
        var source = Geometry.Positions;
        var positions = new Vector3[source.Length];
        var normals = new Vector3[source.Length];

        for (int i = 0; i < source.Length; i++)
        {
            positions[i] = Vector3.TransformVector(source[i] + offset, transform) + CellCenter;
            normals[i] = Vector3.TransformVector(Geometry.Normals[i], transform).Normalized();
        }

        return new BlockShapePose(positions, normals);
    }

    private static Face DominantFace(Vector3 normal)
    {
        float x = MathF.Abs(normal.X);
        float y = MathF.Abs(normal.Y);
        float z = MathF.Abs(normal.Z);

        if (y >= x && y >= z) return normal.Y >= 0f ? Face.Up : Face.Down;
        if (x >= z) return normal.X >= 0f ? Face.Right : Face.Left;
        return normal.Z >= 0f ? Face.Forward : Face.Back;
    }
}
