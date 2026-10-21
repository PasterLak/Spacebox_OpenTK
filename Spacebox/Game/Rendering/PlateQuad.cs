using Engine;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Shapes;

namespace Spacebox.Game.Rendering;

public struct PlatePlacement
{
    public Matrix4 Local;
    public Vector3 Center;
    public Vector3 Normal;
}

public static class PlateQuad
{
    private const float DepthOffsetFactor = -1f;
    private const float DepthOffsetUnits = -16f;

    private static MeshBuffer _quad;

    public static PlatePlacement Place(SpaceEntity entity, Vector3i cell, BlockShape shape, Direction direction, Rotation rotation, Box2 area, float height)
    {
        bool supported = BlockShapes.IsSupported(entity, cell, shape, direction, rotation);
        var origin = new Vector3(cell.X, cell.Y, cell.Z);

        var topLeft = origin + shape.ToCellSpace(new Vector3(area.Min.X, height, area.Min.Y), direction, rotation, supported);
        var topRight = origin + shape.ToCellSpace(new Vector3(area.Max.X, height, area.Min.Y), direction, rotation, supported);
        var bottomLeft = origin + shape.ToCellSpace(new Vector3(area.Min.X, height, area.Max.Y), direction, rotation, supported);

        var right = topRight - topLeft;
        var down = bottomLeft - topLeft;
        var normal = Vector3.Cross(down, right).Normalized();

        return new PlatePlacement
        {
            Normal = normal,
            Center = topLeft + (right + down) * 0.5f,
            Local = new Matrix4(
                new Vector4(right, 0f),
                new Vector4(normal, 0f),
                new Vector4(down, 0f),
                new Vector4(topLeft, 1f))
        };
    }

    public static bool IsVisible(in PlatePlacement placement, Vector3 cameraLocal, float viewDistance)
    {
        var toCamera = cameraLocal - placement.Center;
        if (toCamera.LengthSquared > viewDistance * viewDistance) return false;
        return Vector3.Dot(toCamera, placement.Normal) > 0f;
    }

    public static Vector3 SampleLight(SpaceEntity entity, in PlatePlacement placement)
    {
        return BlockLightSampler.Sample(entity.EntityLocalToWorld(placement.Center + placement.Normal * 0.5f), entity);
    }

    public static void Draw()
    {
        _quad ??= CreateQuad();
        GL.Enable(EnableCap.PolygonOffsetFill);
        GL.PolygonOffset(DepthOffsetFactor, DepthOffsetUnits);
        GL.BindVertexArray(_quad.VAO);
        GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
        GL.Disable(EnableCap.PolygonOffsetFill);
    }

    private static MeshBuffer CreateQuad()
    {
        var buffer = TextureMaterial.GetMeshBuffer();
        Resources.RemoveResourceFromControll(buffer);
        float[] vertices =
        {
            0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f,
            1f, 0f, 0f, 0f, 1f, 0f, 1f, 0f,
            1f, 0f, 1f, 0f, 1f, 0f, 1f, 1f,
            0f, 0f, 1f, 0f, 1f, 0f, 0f, 1f
        };
        uint[] indices = { 0, 1, 2, 2, 3, 0 };
        buffer.BindBuffer(ref vertices, ref indices);
        buffer.SetAttributes();
        return buffer;
    }
}
