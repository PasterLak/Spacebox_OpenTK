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
    private readonly Matrix4[] _transforms = new Matrix4[DirectionCount * RotationCount];
    private readonly Vector3 _supportedOffset;
    private readonly Vector3 _freeOffset;

    public BlockGeometry Geometry { get; }
    public Face[] VertexFaces { get; }

    public BlockShape(BlockGeometry geometry)
    {
        Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
        VertexFaces = new Face[geometry.VertexCount];
        for (int i = 0; i < VertexFaces.Length; i++)
            VertexFaces[i] = DominantFace(geometry.Normals[i]);

        var anchor = geometry.AnchorPoint;
        var bottomCenter = new Vector3(anchor.X, geometry.Min.Y, anchor.Z);
        var supportedOffset = new Vector3(0f, -0.5f, 0f) - bottomCenter;
        var freeOffset = -new Vector3(anchor.X, (geometry.Min.Y + geometry.Max.Y) * 0.5f, anchor.Z);
        _supportedOffset = supportedOffset;
        _freeOffset = freeOffset;

        for (int d = 0; d < DirectionCount; d++)
        {
            for (int r = 0; r < RotationCount; r++)
            {
                var transform = BlockRotationHelper.CalculateTransformMatrix(Direction.Up, (Direction)d, (Rotation)r);
                int index = d * RotationCount + r;
                _transforms[index] = transform;

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

    public bool HasFacing => Geometry.FacingUp.HasValue;
    public bool RequiresSupport => Geometry.RequiresSupport;

    public Matrix4 GetTransform(Direction direction, Rotation rotation)
    {
        return _transforms[(int)direction * RotationCount + (int)rotation];
    }

    public Vector3 ToCellSpace(Vector3 geometryPoint, Direction direction, Rotation rotation, bool supported)
    {
        var offset = supported ? _supportedOffset : _freeOffset;
        return Vector3.TransformVector(geometryPoint + offset, GetTransform(direction, rotation)) + CellCenter;
    }

    public Rotation BestRotation(Direction direction, Vector3 desiredUp)
    {
        if (!Geometry.FacingUp.HasValue) return Rotation.None;

        var facing = Geometry.FacingUp.Value;
        var best = Rotation.None;
        float bestDot = float.MinValue;
        for (int r = 0; r < RotationCount; r++)
        {
            float dot = Vector3.Dot(Vector3.TransformVector(facing, GetTransform(direction, (Rotation)r)), desiredUp);
            if (dot <= bestDot) continue;
            bestDot = dot;
            best = (Rotation)r;
        }
        return best;
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
